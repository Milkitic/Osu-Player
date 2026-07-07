using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIDXToolbox;
using IIDXToolbox.Readers;
using IIDXToolbox.Readers.Charts;

namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Loads the on-disk IIDX chart and audio sample blocks for a single difficulty of
/// one music id. Encapsulates <see cref="ChartParser"/> and <see cref="TwoDxReader"/>/
/// <see cref="S3PReader"/> behind a single awaitable entry point so callers don't
/// have to know the container format.
/// </summary>
public static class IidxResourceLoader
{
    public static async Task<IidxLoadedResources> LoadAsync(
        IidxDataLayout layout,
        int musicId,
        IidxDifficulty difficulty,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);
        cancellationToken.ThrowIfCancellationRequested();

        var chartPath = layout.GetChartPath(musicId);
        if (!File.Exists(chartPath))
        {
            throw new FileNotFoundException(
                $"IIDX chart file not found: {chartPath}", chartPath);
        }

        var audioPath = layout.GetAudioPath(musicId);
        if (audioPath == null)
        {
            throw new FileNotFoundException(
                $"IIDX audio file not found under: {layout.GetSoundFolder(musicId)}");
        }

        Chart? selectedChart;
        await using (var chartStream = new FileStream(chartPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            selectedChart = await Task.Run(() =>
            {
                var charts = ChartParser.Parse(chartStream);
                return SelectChart(charts, difficulty);
            }, cancellationToken).ConfigureAwait(false);
        }

        if (selectedChart == null)
        {
            throw new InvalidOperationException(
                $"IIDX chart for difficulty {difficulty} not found in '{chartPath}'.");
        }

        var audioBlocks = await Task.Run(() => LoadAudioBlocks(audioPath), cancellationToken).ConfigureAwait(false);

        string? thumbnailPath = layout.GetThumbnailPath(musicId);

        return new IidxLoadedResources
        {
            MusicId = musicId,
            Difficulty = difficulty,
            Chart = selectedChart,
            AudioBlocks = audioBlocks,
            AudioPath = audioPath,
            ThumbnailPath = thumbnailPath,
            SoundFolder = layout.GetSoundFolder(musicId),
        };
    }

    private static Chart? SelectChart(List<Chart> charts, IidxDifficulty difficulty)
    {
        var target = MapDifficulty(difficulty);
        foreach (var chart in charts)
        {
            if (chart.ChartDifficulty == target) return chart;
        }

        return null;
    }

    private static ChartDifficulty MapDifficulty(IidxDifficulty difficulty) => difficulty switch
    {
        IidxDifficulty.SpBeginner => ChartDifficulty.SPBeginner,
        IidxDifficulty.SpNormal => ChartDifficulty.SPNormal,
        IidxDifficulty.SpHyper => ChartDifficulty.SPHyper,
        IidxDifficulty.SpAnother => ChartDifficulty.SPAnother,
        IidxDifficulty.SpLegendaria => ChartDifficulty.SPLegendaria,
        IidxDifficulty.DpBeginner => ChartDifficulty.DPBeginner,
        IidxDifficulty.DpNormal => ChartDifficulty.DPNormal,
        IidxDifficulty.DpHyper => ChartDifficulty.DPHyper,
        IidxDifficulty.DpAnother => ChartDifficulty.DPAnother,
        IidxDifficulty.DpLegendaria => ChartDifficulty.DPLegendaria,
        _ => ChartDifficulty.SPAnother
    };

    private static List<ReadOnlyMemory<byte>> LoadAudioBlocks(string audioPath)
    {
        var extension = Path.GetExtension(audioPath).ToLowerInvariant();
        if (extension == ".2dx")
        {
            var reader = new TwoDxReader(audioPath);
            try
            {
                reader.ReadToEnd();
                return new List<ReadOnlyMemory<byte>>(reader.EnumerateExtractedAudio());
            }
            finally
            {
                reader.Dispose();
            }
        }

        if (extension == ".s3p")
        {
            var reader = new S3PReader(audioPath);
            try
            {
                reader.ReadToEnd();
                return new List<ReadOnlyMemory<byte>>(reader.EnumerateExtractedAudio());
            }
            finally
            {
                reader.Dispose();
            }
        }

        throw new NotSupportedException($"Unsupported IIDX audio container: {extension}");
    }
}

public sealed class IidxLoadedResources
{
    public required int MusicId { get; init; }
    public required IidxDifficulty Difficulty { get; init; }
    public required Chart Chart { get; init; }
    public required IReadOnlyList<ReadOnlyMemory<byte>> AudioBlocks { get; init; }
    public required string AudioPath { get; init; }
    public string? ThumbnailPath { get; init; }
    public required string SoundFolder { get; init; }
}