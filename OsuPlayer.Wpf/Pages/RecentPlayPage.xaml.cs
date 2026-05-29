using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages;

/// <summary>
/// RecentPlayPage.xaml 的交互逻辑
/// </summary>
public partial class RecentPlayPage : Page
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly RecentPlayPageViewModel _viewModel;
    private readonly MainWindow _mainWindow;
    private ObservableCollection<Beatmap> _recentBeatmaps;

    public RecentPlayPage(RecentPlayPageViewModel viewModel, IPlayerDataService playerData,
        ObservablePlayController controller)
    {
        _viewModel = viewModel;
        _playerData = playerData;
        _controller = controller;

        InitializeComponent();
        _mainWindow = (MainWindow)Application.Current.MainWindow;
        DataContext = _viewModel;
    }

    public async Task UpdateListAsync()
    {
        _recentBeatmaps = new ObservableCollection<Beatmap>(
            await _playerData.GetBeatmapsByMapInfoAsync(await _playerData.GetRecentListAsync(), TimeSortMode.PlayTime));
        _viewModel.Beatmaps =
            new NumberableObservableCollection<BeatmapDataModel>(await _recentBeatmaps.ToDataModelListAsync());
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await UpdateListAsync();
        var item = _viewModel.Beatmaps.FirstOrDefault(k =>
            k.GetIdentity().Equals(_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
        RecentList.SelectedItem = item;
    }

    private void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        PlaySelected();
    }

    private async void BtnDelAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveAll"), _mainWindow.Title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result == MessageBoxResult.OK)
        {
            if (await _playerData.TryClearRecentAsync())
                await UpdateListAsync();
        }
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        await _controller.PlayList.SetSongListAsync(_recentBeatmaps, true);
    }

    private async void PlaySelected()
    {
        var map = GetSelected();
        if (map == null) return;

        await _controller.PlayNewAsync(map);
    }

    private Beatmap GetSelected()
    {
        if (RecentList.SelectedItem == null)
            return null;
        var selectedItem = (BeatmapDataModel)RecentList.SelectedItem;

        return _recentBeatmaps.FirstOrDefault(k =>
            k.FolderName == selectedItem.FolderName &&
            k.Version == selectedItem.Version
        );
    }

    private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
    {
        return _recentBeatmaps.FirstOrDefault(k =>
            k.FolderName == dataModel.FolderName &&
            k.Version == dataModel.Version
        );
    }

    private IEnumerable<Beatmap> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
    {
        return dataModels.Select(ConvertToEntry);
    }
}