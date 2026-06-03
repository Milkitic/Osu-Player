using Coosu.Beatmap;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Media.Audio;

/// <summary>
/// Carries the result of a beatmap loading pipeline. Contains all resolved
/// file paths, parsed metadata, and derived flags needed by the controller
/// to populate <see cref="BeatmapContext"/> and create the player.
/// </summary>
public sealed class BeatmapLoadResult
{
    public required LocalOsuFile OsuFile { get; init; }
    public required Beatmap Beatmap { get; init; }
    public required string BaseFolder { get; init; }
    public required string MapPath { get; init; }
    public required string MusicPath { get; init; }
    public required string? BackgroundPath { get; init; }
    public required string? VideoPath { get; init; }
    public required bool HasStoryboard { get; init; }
    public required bool IsFavorite { get; init; }
}
