using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Pages;

public class RecentPlayPageVm : VmBase
{
    private ObservableCollection<LoosePlayItem> _playItems;
    private LoosePlayItem _selectedPlayItem;

    public ObservableCollection<LoosePlayItem> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    public LoosePlayItem SelectedPlayItem
    {
        get => _selectedPlayItem;
        set => this.RaiseAndSetIfChanged(ref _selectedPlayItem, value);
    }
}

/// <summary>
/// RecentPlayPage.xaml 的交互逻辑
/// </summary>
public partial class RecentPlayPage : Page
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly RecentPlayPageVm _viewModel;
    private readonly ExportService _exportService;

    public RecentPlayPage()
    {
        DataContext = _viewModel = new RecentPlayPageVm();
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        _exportService = ServiceProviders.Default.GetService<ExportService>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await UpdateList();

        var item = _viewModel.PlayItems?.FirstOrDefault(k =>
            k.PlayItem?.StandardizedPath == _playListService.GetCurrentPath());
        if (item != null)
        {
            DataGrid.SelectedItem = item;
        }
    }

    private void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        PlaySelected();
    }

    private async void BtnDelAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MsgDialog.WarnOkCancel(I18NUtil.GetString("ui-ensureRemoveAll"),
            App.CurrentMainWindow?.Title);
        if (!result) return;

        var appDbContext = ServiceProviders.GetApplicationDbContext();
        await appDbContext.ClearRecentList();
        _viewModel.PlayItems.Clear();
    }

    private async Task UpdateList()
    {
        var appDbContext = ServiceProviders.GetApplicationDbContext();
        var queryResult = await appDbContext.GetRecentListFull();
        // todo: pagination
        _viewModel.PlayItems = new ObservableCollection<LoosePlayItem>(queryResult.Results);
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        var appDbContext = ServiceProviders.GetApplicationDbContext();
        var paginationQueryResult = await appDbContext.GetRecentListFull(0, int.MaxValue);
        if (paginationQueryResult.Results.Count == 0) return;

        await FormUtils.ReplacePlayListAndPlayAll(
            paginationQueryResult.Results.Where(k => !k.IsItemLost).Select(k => k.PlayItem!.StandardizedPath),
            _playListService, _playerService);
    }

    private async void PlaySelected()
    {
        var loosePlayItem = (LoosePlayItem)DataGrid.SelectedItem;
        if (loosePlayItem == null) return;
        if (loosePlayItem.IsItemLost) return;

        var standardizedPath = loosePlayItem.PlayItem!.StandardizedPath;
        await _playerService.InitializeNewAsync(standardizedPath, true);
    }

    private async void MiPlay_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPlayItem is not { IsItemLost: false, PlayItem: { } playItem } loosePlayItem) return;

        await _playerService.InitializeNewAsync(playItem.StandardizedPath, true);
    }

    private void MiConditionSearch_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string type }) return;

        var keyword = type switch
        {
            "Title" => _viewModel.SelectedPlayItem.Title,
            "Artist" => _viewModel.SelectedPlayItem.Artist,
            "Creator" => _viewModel.SelectedPlayItem.Creator,
            "Source" => _viewModel.SelectedPlayItem.PlayItem?.PlayItemDetail?.Creator,
            _ => null
        };
        if (keyword is null) return;

        App.CurrentMainWindow
            .NavigationBar
            .SwitchSearch
            .CheckAndAction(page => ((SearchPage)page).Search(keyword));
    }

    private void MiOpenSourceFolder_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPlayItem is not { IsItemLost: false, PlayItem: { } playItem } loosePlayItem) return;

        var folder = PathUtils.GetFullPath(playItem.StandardizedFolder, AppSettings.Default.GeneralSection.OsuSongDir);
        if (!Directory.Exists(folder))
        {
            Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
            return;
        }

        ProcessUtils.StartWithShellExecute(folder);
    }

    private void MiOpenScorePage_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPlayItem is not { IsItemLost: false, PlayItem: { } playItem } loosePlayItem) return;

        ProcessUtils.StartWithShellExecute($"https://osu.ppy.sh/s/{playItem.PlayItemDetail.BeatmapSetId}");
    }

    private void MiSaveToPlayList_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPlayItem is not { IsItemLost: false, PlayItem: { } playItem } loosePlayItem) return;

        App.CurrentMainContentDialog.ShowContent(new SelectPlayListControl(playItem),
            DialogOptionFactory.SelectPlayListOptions);
    }

    private void MiExport_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPlayItem is not { IsItemLost: false, PlayItem: { } playItem } loosePlayItem) return;

        _exportService.QueueBeatmap(playItem);
    }

    private async void MiDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedPlayItem is not { IsItemLost: false, PlayItem: { } playItem } loosePlayItem) return;

        //var appDbContext = ServiceProviders.GetApplicationDbContext();
        //await appDbContext.RemoveBeatmapFromRecent(loosePlayItem);
        //_viewModel.PlayItems.Remove(loosePlayItem);
    }
}