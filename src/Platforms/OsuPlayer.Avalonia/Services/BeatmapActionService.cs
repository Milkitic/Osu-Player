#nullable enable

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Playback;
using OsuPlayer.Shared;

namespace OsuPlayer.Services;

public sealed class BeatmapActionService : IBeatmapActionService
{
    private const string MissingFolderMessage = "The selected file does not exist, possibly not synced. Try syncing osuDB again.";

    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly IExportService _exportService;
    private readonly IBeatmapDifficultyPicker _difficultyPicker;
    private readonly IAppNotificationService _notifications;

    public BeatmapActionService(
        IPlayerDataService playerData,
        ObservablePlayController controller,
        IExportService exportService,
        IBeatmapDifficultyPicker difficultyPicker,
        IAppNotificationService notifications)
    {
        _playerData = playerData;
        _controller = controller;
        _exportService = exportService;
        _difficultyPicker = difficultyPicker;
        _notifications = notifications;
    }

    public async Task<Beatmap?> GetHighestDifficultyAsync(IMapIdentifiable? beatmap)
    {
        if (beatmap == null) return null;
        return (await _playerData.GetBeatmapsFromFolderAsync(beatmap.FolderName, beatmap.SourceGame)).GetHighestDiff();
    }

    public async Task OpenSourceFolderAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false)
    {
        if (beatmap == null) return;
        if (await ResolveAsync(beatmap, highestDifficulty) == null) return;

        var folder = beatmap.GetFolder(out _, out _);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            _notifications.Push(MissingFolderMessage);
            return;
        }

        StartProcess(folder);
    }

    public async Task OpenScorePageAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false)
    {
        var map = await ResolveAsync(beatmap, highestDifficulty);
        if (map == null) return;
        StartProcess($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
    }

    public async Task SaveToCollectionAsync(IMapIdentifiable? beatmap)
    {
        var map = await ResolveAsync(beatmap, highestDifficulty: false);
        if (map == null) return;
        await FrontDialogService.ShowSelectCollectionAsync(null, map);
    }

    public async Task SaveToCollectionWithDifficultyPickerAsync(IMapIdentifiable? beatmap)
    {
        var selected = await PickDifficultyAsync(beatmap);
        if (selected != null)
        {
            await FrontDialogService.ShowSelectCollectionAsync(null, selected);
        }
    }

    public async Task ExportAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false)
    {
        var map = await ResolveAsync(beatmap, highestDifficulty);
        if (map == null) return;
        _exportService.QueueEntry(map);
    }

    public async Task PlayAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false, bool playInstantly = true)
    {
        var map = await ResolveAsync(beatmap, highestDifficulty);
        if (map == null) return;
        await _controller.PlayNewAsync(map, playInstantly);
    }

    public async Task PlayWithDifficultyPickerAsync(IMapIdentifiable? beatmap)
    {
        var selected = await PickDifficultyAsync(beatmap);
        if (selected != null)
        {
            await _controller.PlayNewAsync(selected, true);
        }
    }

    private async Task<Beatmap?> ResolveAsync(IMapIdentifiable? beatmap, bool highestDifficulty)
    {
        if (beatmap == null) return null;
        return highestDifficulty
            ? await GetHighestDifficultyAsync(beatmap)
            : await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
    }

    private async Task<Beatmap?> PickDifficultyAsync(IMapIdentifiable? beatmap)
    {
        if (beatmap == null) return null;
        var beatmaps = await _playerData.GetBeatmapsFromFolderAsync(beatmap.FolderName, beatmap.SourceGame);
        return await _difficultyPicker.PickAsync(beatmaps);
    }

    private static void StartProcess(string target)
    {
        Process.Start(new ProcessStartInfo(target)
        {
            UseShellExecute = true
        });
    }
}
