namespace OsuPlayer.Media.Audio;

/// <summary>
/// Immutable description of a beatmap's on-disk resources. Used to wire
/// resolved paths into <see cref="OsuAudioSessionOptions"/> without dragging
/// along the broader <see cref="BeatmapLoadResult"/> surface (parses,
/// favourite flag, storyboard flag, etc.).
/// </summary>
/// <remarks>
/// Lives at the boundary between the loading pipeline and the audio
/// session. Both <see cref="BeatmapLoader"/> (which resolves the paths) and
/// <see cref="OsuMixPlayer"/> (which builds session options) consume this
/// type, so neither has to translate field-by-field into the other.
/// </remarks>
public sealed class BeatmapResources
{
    /// <summary>
    /// Folder containing the beatmap's <c>.osu</c> file and audio.
    /// </summary>
    public required string BeatmapFolder { get; init; }

    /// <summary>
    /// Filename of the <c>.osu</c> file within <see cref="BeatmapFolder"/>.
    /// May be empty when the beatmap was opened by absolute path.
    /// </summary>
    public required string BeatmapFilename { get; init; }

    /// <summary>
    /// Filename of the audio asset (e.g. <c>audio.mp3</c>) within
    /// <see cref="BeatmapFolder"/>.
    /// </summary>
    public required string AudioFilename { get; init; }

    /// <summary>
    /// Path to the user skin folder used to resolve skin-sourced samples.
    /// </summary>
    public required string UserSkinFolder { get; init; }

    /// <summary>
    /// Path to the bundled default hitsound folder, used as the last-resort
    /// fallback for skin-less sample resolution.
    /// </summary>
    public required string DefaultHitsoundFolder { get; init; }
}
