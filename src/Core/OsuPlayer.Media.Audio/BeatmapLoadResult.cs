using Coosu.Beatmap;
using OsuPlayer.Data.Models;
using OsuPlayer.Iidx.Abstractions;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Carries the result of a beatmap loading pipeline. Contains all resolved
/// file paths, parsed metadata, and derived flags needed by the controller
/// to populate <see cref="Playlist.BeatmapContext"/> and create the player.
/// </summary>
/// <remarks>
/// For osu!-sourced beatmaps, <see cref="OsuFile"/> is set and
/// <see cref="IidxResources"/> is null. For IIDX-sourced beatmaps, the reverse:
/// <see cref="IidxResources"/> is set and <see cref="OsuFile"/> is null.
/// </remarks>
public sealed class BeatmapLoadResult
{
    public LocalOsuFile? OsuFile { get; init; }
    public IidxLoadedResources? IidxResources { get; init; }
    public required Beatmap Beatmap { get; init; }
    public required string BaseFolder { get; init; }
    public required string MapPath { get; init; }
    public required string? MusicPath { get; init; }
    public required string? BackgroundPath { get; init; }
    public required string? VideoPath { get; init; }
    public required bool HasStoryboard { get; init; }
    public required bool IsFavorite { get; init; }
}
