#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Media.Audio;
using OsuPlayer.Shared;
using OsuPlayer.UiComponents.FrontDialogComponent;
using OsuPlayer.UiComponents.NotificationComponent;
using OsuPlayer.UserControls;

namespace OsuPlayer.Services;

public sealed class BeatmapActionService : IBeatmapActionService
{
    private const string MissingFolderMessage = "所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。";

    private readonly IPlayerDataService _playerData;
    private readonly ObservablePlayController _controller;
    private readonly IExportService _exportService;

    public BeatmapActionService(
        IPlayerDataService playerData,
        ObservablePlayController controller,
        IExportService exportService)
    {
        _playerData = playerData;
        _controller = controller;
        _exportService = exportService;
    }

    public async Task<Beatmap?> GetHighestDifficultyAsync(IMapIdentifiable? beatmap)
    {
        if (beatmap == null) return null;
        return (await _playerData.GetBeatmapsFromFolderAsync(beatmap.FolderName)).GetHighestDiff();
    }

    public async Task OpenSourceFolderAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false)
    {
        if (beatmap == null) return;
        if (await ResolveAsync(beatmap, highestDifficulty) == null) return;

        var folder = beatmap.GetFolder(out _, out _);
        if (!Directory.Exists(folder))
        {
            Notification.Push(MissingFolderMessage);
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
        ShowSelectCollection(map);
    }

    public async Task SaveToCollectionWithDifficultyPickerAsync(IMapIdentifiable? beatmap)
    {
        await ShowDifficultyPickerAsync(beatmap, (selected, arg) =>
        {
            arg.Handled = true;
            ShowSelectCollection(selected);
            return Task.CompletedTask;
        });
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
        await ShowDifficultyPickerAsync(beatmap, async (selected, _) =>
        {
            await _controller.PlayNewAsync(selected, true);
        });
    }

    private async Task<Beatmap?> ResolveAsync(IMapIdentifiable? beatmap, bool highestDifficulty)
    {
        if (beatmap == null) return null;
        return highestDifficulty
            ? await GetHighestDifficultyAsync(beatmap)
            : await _playerData.GetBeatmapByIdentifiableAsync(beatmap);
    }

    private async Task ShowDifficultyPickerAsync(
        IMapIdentifiable? beatmap,
        Func<Beatmap, CallbackObj, Task> onSelect)
    {
        if (beatmap == null) return;
        var beatmaps = await _playerData.GetBeatmapsFromFolderAsync(beatmap.GetIdentity().FolderName);
        if (beatmaps.Count == 0) return;

        FrontDialogOverlay.Default.ShowContent(
            new DiffSelectControl(beatmaps, onSelect),
            DialogOptionFactory.DiffSelectOptions);
    }

    private static void ShowSelectCollection(Beatmap map)
    {
        FrontDialogOverlay.Default.ShowContent(
            new SelectCollectionControl(map),
            DialogOptionFactory.SelectCollectionOptions);
    }

    private static void StartProcess(string target)
    {
        Process.Start(new ProcessStartInfo(target)
        {
            UseShellExecute = true
        });
    }
}
