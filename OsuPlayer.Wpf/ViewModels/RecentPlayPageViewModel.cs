using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.ViewModels;

public partial class RecentPlayPageViewModel : ObservableObject
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;

    public RecentPlayPageViewModel(IPlayerDataService playerData, ObservablePlayController controller)
    {
        _playerData = playerData;
        _controller = controller;
    }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> Beatmaps { get; set; }

    [RelayCommand]
    private void SearchByCondition(string param)
    {
        WindowEx.GetCurrentFirst<MainWindow>()
            .SwitchSearch
            .CheckAndAction(page => ((SearchPage)page).Search(param));
    }

    [RelayCommand]
    private async Task OpenSourceFolderAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);

        if (map == null) return;
        var folder = beatmap.GetFolder(out _, out _);
        if (!Directory.Exists(folder))
        {
            Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
            return;
        }

        Process.Start(folder);
    }

    [RelayCommand]
    private async Task OpenScorePageAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
    }

    [RelayCommand]
    private async Task SaveCollectionAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(map),
            DialogOptionFactory.SelectCollectionOptions);
    }

    [RelayCommand]
    private async Task ExportAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        ExportPage.QueueEntry(map);
    }

    [RelayCommand]
    private async Task DirectPlayAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        await _controller.PlayNewAsync(map);
    }

    [RelayCommand]
    private async Task PlayAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        var map = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
        if (map == null) return;
        await _controller.PlayNewAsync(map);
    }

    [RelayCommand]
    private async Task RemoveAsync(BeatmapDataModel beatmap)
    {
        if (beatmap == null) return;
        if (await _playerData.TryRemoveFromRecentAsync(beatmap.GetIdentity()))
        {
            Beatmaps.Remove(beatmap);
        }
    }
}
