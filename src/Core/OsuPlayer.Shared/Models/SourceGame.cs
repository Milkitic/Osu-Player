namespace OsuPlayer.Shared;

/// <summary>
/// Identifies the rhythm-game platform a <see cref="IMapIdentifiable"/> originates from.
/// Used to disambiguate osu! and IIDX entries that share the same <see cref="MapIdentity"/>.
/// </summary>
public enum SourceGame
{
    /// <summary>
    /// Default value for legacy rows created before multi-platform support.
    /// Treated as osu! for backward compatibility.
    /// </summary>
    Osu = 0,

    /// <summary>
    /// Beatmania IIDX series (music_data.bin sourced entries).
    /// </summary>
    Iidx = 1
}