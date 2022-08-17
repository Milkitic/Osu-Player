using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.PanelComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Windows;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

/// <summary>
/// CollectionPage.xaml 的交互逻辑
/// </summary>
public partial class CollectionPage : Page
{
    private readonly MainWindow _mainWindow;
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;

    private List<PlayItem> _playItems;

    public CollectionPageViewModel ViewModel { get; set; }
    public string Id { get; set; }

    public CollectionPage()
    {
        InitializeComponent();
        _mainWindow = (MainWindow)Application.Current.MainWindow;
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        ViewModel = (CollectionPageViewModel)this.DataContext;
    }

    public async ValueTask UpdateView(PlayList playList)
    {
        ViewModel.PlayList = playList;
        await UpdateList();
    }

    public async Task UpdateList()
    {
        await using var dbContext = new ApplicationDbContext();
        var playListDetail = await dbContext.PlayLists
            .Include(k => k.PlayListRelations)
            .ThenInclude(k => k.PlayItem)
            .FirstOrDefaultAsync(k => k.Id == ViewModel.PlayList.Id);
        if (playListDetail == null)
        {
            ViewModel.DataList = null;
            return;
        }

        _playItems = playListDetail.PlayListRelations
            .OrderByDescending(k => k.CreateTime)
            .Select(k => k.PlayItem).ToList();

        ViewModel.DataList = new ObservableCollection<PlayItem>(_playItems);
        ListCount.Content = ViewModel.DataList.Count;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var item = ViewModel.DataList?.FirstOrDefault(k => k.StandardizedPath == _playListService.GetCurrentPath());
        if (item != null)
        {
            MapCardList.SelectedItem = item;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = SearchBox.Text.Trim();
        ViewModel.DataList = string.IsNullOrWhiteSpace(keyword)
            ? ViewModel.DataList
            : new ObservableCollection<PlayItem>(ViewModel.DataList); // todo: search
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    private async void BtnDelCol_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveCollection"), _mainWindow.Title, MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result != MessageBoxResult.OK) return;
        await using var dbContext = new ApplicationDbContext();

        dbContext.Remove(ViewModel.PlayList);
        await dbContext.SaveChangesAsync();

        _mainWindow.SwitchRecent.IsChecked = true;
        await _mainWindow.UpdatePlayLists();
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
        await using var dbContext = new ApplicationDbContext();
        var beatmaps = await dbContext.GetPlayItemsByFolderAsync(selectedItem.StandardizedFolder);
        return beatmaps.FirstOrDefault(k => k.PlayItemDetail.Version == selectedItem.PlayItemDetail.Version);
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.PlayList),
            DialogOptionFactory.EditCollectionOptions);
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        var playItems = _playItems.ToList();
        if (playItems.Count <= 0) return;

        _playListService.SetPathList(playItems.Select(k => k.StandardizedPath), false);
        await _playerService.PlayNextAsync();
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
    {
        var playItem = ViewModel.DataList[e.Index];
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