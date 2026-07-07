using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Iidx.Abstractions;
using OsuPlayer.Shared;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Loads a database-backed IIDX beatmap into a <see cref="BeatmapLoadResult"/>.
/// Unlike the osu! path, this does not produce an <see cref="Coosu.Beatmap.LocalOsuFile"/>;
/// instead it populates <see cref="BeatmapLoadResult.IidxResources"/> with the
/// parsed chart and decoded 2dx audio blocks so the controller can wire an
/// <see cref="IidxMixPlayer"/>.
/// </summary>
public sealed class IidxBeatmapLoader
{
    private readonly IPlayerDataStore _playerData;
    private readonly ILogger _logger;

    public IidxBeatmapLoader(IPlayerDataStore playerData, ILogger logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    public async Task<BeatmapLoadResult> LoadFromBeatmapAsync(
        Beatmap beatmap,
        BeatmapSettings? beatmapSettings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var musicDataPath = AppSettings.Default?.General.IidxMusicDataPath;
        if (string.IsNullOrWhiteSpace(musicDataPath) || !File.Exists(musicDataPath))
        {
            throw new InvalidDataException(
                "IIDX music_data.bin path has not been configured. Open Settings → General to set it.");
        }

        var layout = IidxDataLayout.FromMusicDataPath(musicDataPath);
        var musicId = beatmap.IidxMusicId;
        if (musicId <= 0)
        {
            throw new InvalidDataException(
                $"Beatmap '{beatmap.AutoTitle}' is marked IIDX but has no IidxMusicId.");
        }

        var difficulty = MapDifficulty(beatmap);
        _logger.LogInformation(
            "Loading IIDX beatmap: musicId={MusicId} difficulty={Difficulty}", musicId, difficulty);

        var resources = await IidxResourceLoader.LoadAsync(layout, musicId, difficulty, cancellationToken)
            .ConfigureAwait(false);

        var trueBeatmap = await ResolveBeatmapAsync(beatmap).ConfigureAwait(false);
        var isFavorite = await CheckIsFavoriteAsync(beatmapSettings).ConfigureAwait(false);

        var backgroundPath = resources.ThumbnailPath
            ?? BeatmapPathResolver.GetDefaultImagePath(AppPaths.Current.ResourcePath);

        return new BeatmapLoadResult
        {
            OsuFile = null,
            IidxResources = resources,
            Beatmap = trueBeatmap,
            BaseFolder = resources.SoundFolder,
            MapPath = layout.GetChartPath(musicId),
            MusicPath = resources.AudioPath,
            BackgroundPath = File.Exists(backgroundPath) ? backgroundPath : null,
            VideoPath = null,
            HasStoryboard = false,
            IsFavorite = isFavorite,
        };
    }

    private static IidxDifficulty MapDifficulty(Beatmap beatmap)
    {
        // The version label is the short difficulty label (SPB/SPN/...) set by IidxBeatmapFactory.
        var labels = IidxDifficultyLabels.AllLabels;
        var index = -1;
        for (var i = 0; i < labels.Count; i++)
        {
            if (labels[i] == beatmap.Version)
            {
                index = i;
                break;
            }
        }

        return index switch
        {
            0 => IidxDifficulty.SpBeginner,
            1 => IidxDifficulty.SpNormal,
            2 => IidxDifficulty.SpHyper,
            3 => IidxDifficulty.SpAnother,
            4 => IidxDifficulty.SpLegendaria,
            5 => IidxDifficulty.DpBeginner,
            6 => IidxDifficulty.DpNormal,
            7 => IidxDifficulty.DpHyper,
            8 => IidxDifficulty.DpAnother,
            9 => IidxDifficulty.DpLegendaria,
            _ => IidxDifficulty.SpAnother
        };
    }

    private async Task<Beatmap> ResolveBeatmapAsync(Beatmap parsed)
    {
        var fromDb = await _playerData.GetBeatmapByIdentifiableAsync(parsed).ConfigureAwait(false);
        return fromDb ?? parsed;
    }

    private async Task<bool> CheckIsFavoriteAsync(BeatmapSettings? settings)
    {
        if (settings == null) return false;

        var album = await _playerData.GetCollectionsByMapAsync(settings).ConfigureAwait(false);
        return album != null && album.Count > 0 && album.Any(k => k.LockedBool);
    }
}