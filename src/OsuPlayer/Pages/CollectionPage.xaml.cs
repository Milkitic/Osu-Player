using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.PanelComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

/// <summary>
/// CollectionPage.xaml 的交互逻辑
/// </summary>
public partial class CollectionPage : Page
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly CollectionPageViewModel _viewModel;

    private List<PlayItem> _playItems;
    private bool _firstLoaded;

    public CollectionPage(PlayList playList)
    {
        InitializeComponent();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        DataContext = _viewModel = new CollectionPageViewModel();
        _viewModel.PlayList = playList;
    }

    public async Task UpdateList()
    {
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var playListDetail = await dbContext.PlayLists
            .Include(k => k.PlayListRelations)
            .ThenInclude(k => k.PlayItem)
            .FirstOrDefaultAsync(k => k.Id == _viewModel.PlayList.Id);
        if (playListDetail == null)
        {
            _viewModel.PlayItems = null;
            return;
        }

        _playItems = playListDetail.PlayListRelations
            .OrderByDescending(k => k.CreateTime)
            .Select(k => k.PlayItem).ToList();

        _viewModel.PlayItems = new ObservableCollection<PlayItem>(_playItems);
        ListCount.Content = _viewModel.PlayItems.Count;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_firstLoaded)
        {
            await UpdateList();
            _firstLoaded = true;
        }

        var item = _viewModel.PlayItems?.FirstOrDefault(k => k.StandardizedPath == _playListService.GetCurrentPath());
        if (item != null)
        {
            MapCardList.SelectedItem = item;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchBox.Text.Trim();
        _viewModel.PlayItems = string.IsNullOrWhiteSpace(keyword)
            ? _viewModel.PlayItems
            : new ObservableCollection<PlayItem>(_viewModel.PlayItems); // todo: search
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    private async void BtnDelCol_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(I18NUtil.GetString("ui-ensureRemoveCollection"),
            App.Current.MainWindow?.Title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result != MessageBoxResult.OK) return;
        await using var dbContext = ServiceProviders.GetApplicationDbContext();

        dbContext.Remove(_viewModel.PlayList);
        await dbContext.SaveChangesAsync();

        await SharedVm.Default.UpdatePlayListsAsync();
        SharedVm.Default.CheckedNavigationType = NavigationType.Recent;
    }

    private void BtnExportAll_Click(object sender, RoutedEventArgs e)
    {
        ExportPage.QueueBeatmaps(_playItems);
    }

    private async Task PlaySelected()
    {
        var map = await GetSelected();
        if (map == null) return;
        await _playerService.InitializeNewAsync(map.StandardizedPath, true);
    }

    private async Task<PlayItem> GetSelected()
    {
        if (MapCardList.SelectedItem == null)
        {
            return null;
        }

        var selectedItem = (PlayItem)MapCardList.SelectedItem;
        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        var beatmaps = await dbContext.GetPlayItemsByFolderAsync(selectedItem.StandardizedFolder);
        return beatmaps.FirstOrDefault(k => k.PlayItemDetail.Version == selectedItem.PlayItemDetail.Version);
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(_viewModel.PlayList),
            DialogOptionFactory.EditCollectionOptions);
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        if (_playItems.Count <= 0) return;
        await FormUtils.ReplacePlayListAndPlayAll(_playItems.Select(k => k.StandardizedPath),
            _playListService, _playerService);
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
    {
        var playItem = _viewModel.PlayItems[e.Index];
        try
        {
            var fileName = await CommonUtils.GetThumbByBeatmapDbId(playItem).ConfigureAwait(false);
            Execute.OnUiThread(() => playItem.PlayItemAsset!.FullThumbPath = fileName);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while loading panel item.", ex);
        }

        LogTo.Debug(() => $"VirtualizingGalleryWrapPanel: {e.Index}");
    }

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
    }
}