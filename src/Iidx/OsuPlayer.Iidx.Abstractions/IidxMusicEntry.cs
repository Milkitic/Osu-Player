namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Normalized music metadata entry extracted from an IIDX <c>music_data.bin</c>.
/// Platform-agnostic: contains only the fields the player UI and database need,
/// decoupled from the on-disk <c>MusicDbEntry32</c> struct layout.
/// </summary>
public sealed class IidxMusicEntry
{
    public int MusicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleRoman { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string BgaFilename { get; set; } = string.Empty;

    /// <summary>
    /// Signed short. <c>-1</c> indicates an omni-banked entry.
    /// </summary>
    public short Version { get; set; }
    public short OtherFolder { get; set; }
    public short BemaniFolder { get; set; }
    public short SwitchableDiff { get; set; }

    /// <summary>
    /// Difficulty levels (0-12 typical). Index follows <see cref="IidxDifficulty"/> order.
    /// </summary>
    public byte[] DifficultyLevels { get; set; } = new byte[10];

    /// <summary>
    /// Note counts per difficulty. Index follows <see cref="IidxDifficulty"/> order.
    /// </summary>
    public int[] NoteCounts { get; set; } = new int[10];

    /// <summary>
    /// 2dx file identifier per difficulty. Index follows <see cref="IidxDifficulty"/> order.
    /// </summary>
    public byte[] FileIdentifiers { get; set; } = new byte[10];

    /// <summary>
    /// Radar data per difficulty: notes / peak / scratch / soflan / charge / chord.
    /// Outer index follows <see cref="IidxDifficulty"/>; inner length is 6.
    /// </summary>
    public IidxRadarData[] RadarData { get; set; } = new IidxRadarData[10];

    /// <summary>
    /// BGM volume override (0x00-0xFF).
    /// </summary>
    public int BgmVolume { get; set; }

    public short BgaDelay { get; set; }
    public int TitleFontType { get; set; }
    public bool TitleImg { get; set; }
    public bool ArtistImg { get; set; }
    public bool GenreImg { get; set; }
    public bool BannerImg { get; set; }
    public bool PrepareSceneTitleImg { get; set; }

    public string[]? LayersFlag { get; set; }
}