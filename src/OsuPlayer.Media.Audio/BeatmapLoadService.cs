using System;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Playlist;

namespace Milky.OsuPlayer.Media.Audio;

/// <summary>
/// Orchestrates the full beatmap loading pipeline: file I/O through
/// <see cref="BeatmapLoader"/>, metadata resolution, and context population.
/// Player lifecycle remains with <see cref="ObservablePlayController"/> so
/// event wiring and UI orchestration stay explicit.
/// </summary>
internal sealed class BeatmapLoadService
{
    private readonly BeatmapLoader _beatmapLoader;

    public BeatmapLoadService(BeatmapLoader beatmapLoader)
    {
        _beatmapLoader = beatmapLoader;
    }

    /// <summary>
    /// Loads a beatmap from the database-backed <see cref="Beatmap"/> record.
    /// </summary>
    public async Task<BeatmapLoadResult> LoadFromBeatmapAsync(
        BeatmapContext context,
        CancellationToken cancellationToken)
    {
        var loadResult = await _beatmapLoader.LoadFromBeatmapAsync(
            context.Beatmap, context.BeatmapSettings, cancellationToken).ConfigureAwait(false);

        ApplyToContext(context, loadResult, cancellationToken);
        return loadResult;
    }

    /// <summary>
    /// Loads a beatmap from a pre-parsed <see cref="OsuFile"/> (e.g. file-open path).
    /// </summary>
    public async Task<BeatmapLoadResult> LoadFromOsuFileAsync(
        BeatmapContext context,
        string mapPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context.OsuFile);

        var loadResult = await _beatmapLoader.LoadFromOsuFileAsync(
            context.OsuFile, mapPath, context.BeatmapSettings, cancellationToken).ConfigureAwait(false);

        ApplyToContext(context, loadResult, cancellationToken);
        return loadResult;
    }

    private static void ApplyToContext(
        BeatmapContext context,
        BeatmapLoadResult loadResult,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        context.OsuFile = loadResult.OsuFile;

        var metadata = context.BeatmapDetail.Metadata;
        metadata.IsFavorite = loadResult.IsFavorite;
        metadata.ApplyFrom(loadResult.OsuFile);

        context.BeatmapDetail.BaseFolder = loadResult.BaseFolder;
        context.BeatmapDetail.MapPath = loadResult.MapPath;
        context.BeatmapDetail.BackgroundPath = loadResult.BackgroundPath;
        context.BeatmapDetail.MusicPath = loadResult.MusicPath;
    }
}
