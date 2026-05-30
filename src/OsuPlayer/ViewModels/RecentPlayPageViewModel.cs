using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UserControls;

namespace Milky.OsuPlayer.ViewModels;

public partial class RecentPlayPageViewModel : ObservableObject
{
    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly IExportService _exportService;

    public RecentPlayPageViewModel(IPlayerDataService playerData, ObservablePlayController controller, IExportService exportService)
    {
        _playerData = playerData;
        _controller = controller;
        _exportService = exportService;
    }

    [ObservableProperty]
    public partial NumberableObservableCollection<BeatmapDataModel> Beatmaps { get; set; }

    [RelayCommand]
    private void SearchByCondition(string param)
    {
        WeakReferenceMessenger.Default.Send(new SearchRequestedMessage(param));
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
        _exportService.QueueEntry(map);
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
    public async Task PlayAsync(BeatmapDataModel beatmap)
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

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        var recentList = await _playerData.GetRecentListAsync();
        var recentBeatmaps = await _playerData.GetBeatmapsByMapInfoAsync(recentList, TimeSortMode.PlayTime);
        if (recentBeatmaps == null || !recentBeatmaps.Any()) return;

        await _controller.PlayList.SetSongListAsync(recentBeatmaps, true);
    }

    [RelayCommand]
    public async Task ClearAllRecentAsync()
    {
        if (await _playerData.TryClearRecentAsync())
        {
            Beatmaps?.Clear();
        }
    }

    public async Task UpdateListAsync()
    {
        var recentList = await _playerData.GetRecentListAsync();
        var recentBeatmaps = await _playerData.GetBeatmapsByMapInfoAsync(recentList, TimeSortMode.PlayTime);
        Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(await recentBeatmaps.ToDataModelListAsync());
    }
}
