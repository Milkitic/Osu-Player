using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Services;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Media.Audio;

/// <summary>
/// Handles beatmap data loading: file I/O, metadata parsing, path resolution,
/// and favorite status lookup.
/// </summary>
public sealed class BeatmapLoader
{
    private readonly IPlayerDataStore _playerData;
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    public BeatmapLoader(IPlayerDataStore playerData)
    {
        _playerData = playerData;
    }

    /// <summary>
    /// Loads beatmap data starting from a database-backed <see cref="Beatmap"/> record.
    /// Resolves the .osu file on disk from the beatmap's folder information.
    /// </summary>
    public async Task<BeatmapLoadResult> LoadFromBeatmapAsync(
        Beatmap beatmap,
        BeatmapSettings? beatmapSettings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var folder = (beatmap.GetFolder(out var isFromDb, out var freePath) ?? string.Empty).Trim();
        var mapPath = BeatmapPathResolver.ResolveBeatmapPath(folder, beatmap.BeatmapFileName, isFromDb, freePath);
        var baseFolder = Path.GetDirectoryName(mapPath) ?? string.Empty;

        s_logger.Info("Loading beatmap from database: {0}", beatmap.BeatmapFileName);

        var osuFile = await OsuFile.ReadFromFileAsync(mapPath, options => options.ExcludeSection("Editor"))
            .ConfigureAwait(false);

        var trueBeatmap = await ResolveBeatmapAsync(beatmap, mapPath).ConfigureAwait(false);

        return await BuildResultAsync(osuFile, trueBeatmap, baseFolder, mapPath, beatmapSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Loads beatmap data starting from an <see cref="OsuFile"/> that has already been parsed
    /// (e.g. when the user opened a file by path).
    /// </summary>
    public async Task<BeatmapLoadResult> LoadFromOsuFileAsync(
        LocalOsuFile osuFile,
        string mapPath,
        BeatmapSettings? beatmapSettings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var baseFolder = Path.GetDirectoryName(mapPath) ?? string.Empty;
        var beatmap = BeatmapExtension.ParseFromOSharp(osuFile);
        var trueBeatmap = await ResolveBeatmapAsync(beatmap, mapPath).ConfigureAwait(false);

        return await BuildResultAsync(osuFile, trueBeatmap, baseFolder, mapPath, beatmapSettings, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<BeatmapLoadResult> BuildResultAsync(
        LocalOsuFile osuFile,
        Beatmap beatmap,
        string baseFolder,
        string mapPath,
        BeatmapSettings? beatmapSettings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isFavorite = await CheckIsFavoriteAsync(beatmapSettings).ConfigureAwait(false);

        var audioFilename = osuFile.General?.AudioFilename ?? string.Empty;
        var musicPath = BeatmapPathResolver.ResolveChildPath(baseFolder, audioFilename);
        var defaultImagePath = BeatmapPathResolver.GetDefaultImagePath(Domain.ResourcePath);
        var backgroundPath = BeatmapPathResolver.ResolveBackgroundPath(
            baseFolder, osuFile.Events?.BackgroundInfo?.Filename, defaultImagePath);

        string? videoPath = null;
        var videoFilename = osuFile.Events?.VideoInfo?.Filename;
        if (videoFilename != null)
        {
            var resolved = BeatmapPathResolver.TryResolveChildPath(baseFolder, videoFilename);
            if (resolved != null && File.Exists(resolved))
            {
                videoPath = resolved;
            }
        }

        var hasStoryboard = !string.IsNullOrWhiteSpace(osuFile.Events?.StoryboardText) ||
                            StoryboardFileHelper.HasOsbStoryboard(osuFile, mapPath);

        return new BeatmapLoadResult
        {
            OsuFile = osuFile,
            Beatmap = beatmap,
            BaseFolder = baseFolder,
            MapPath = mapPath,
            MusicPath = musicPath,
            BackgroundPath = backgroundPath,
            VideoPath = videoPath,
            HasStoryboard = hasStoryboard,
            IsFavorite = isFavorite,
        };
    }

    private async Task<Beatmap> ResolveBeatmapAsync(Beatmap parsed, string mapPath)
    {
        var fromDb = await _playerData.GetBeatmapByIdentifiableAsync(parsed).ConfigureAwait(false);
        if (fromDb != null)
        {
            return fromDb;
        }

        parsed.FolderName = mapPath;
        return parsed;
    }

    private async Task<bool> CheckIsFavoriteAsync(BeatmapSettings? settings)
    {
        if (settings == null) return false;

        var album = await _playerData.GetCollectionsByMapAsync(settings).ConfigureAwait(false);
        return album != null && album.Count > 0 && album.Any(k => k.LockedBool);
    }
}
