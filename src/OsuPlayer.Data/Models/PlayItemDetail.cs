using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Coosu.Beatmap.Sections.GamePlay;
using Microsoft.EntityFrameworkCore;

namespace Milki.OsuPlayer.Data.Models;

[Index(nameof(Artist), nameof(ArtistUnicode),
    nameof(Title), nameof(TitleUnicode),
    nameof(Creator), nameof(Source), nameof(Tags)
)]
[Index(nameof(Artist))]
[Index(nameof(ArtistUnicode))]
[Index(nameof(Title))]
[Index(nameof(TitleUnicode))]
[Index(nameof(Creator))]
[Index(nameof(Source))]
[Index(nameof(Tags))]
public sealed class PlayItemDetail
{
    public int Id { get; set; }
    [MaxLength(256)] public string Artist { get; set; } = null!;
    [MaxLength(256)] public string ArtistUnicode { get; set; } = null!;
    [MaxLength(256)] public string Title { get; set; } = null!;
    [MaxLength(256)] public string TitleUnicode { get; set; } = null!;
    [MaxLength(64)] public string Creator { get; set; } = null!;
    [MaxLength(128)] public string Version { get; set; } = null!;
    [MaxLength(128)] public string BeatmapFileName { get; set; } = null!;
    [MaxLength(128)] public GameMode GameMode { get; set; }
    //public DateTime LastModified { get; set; }

    /// <summary>
    /// Should / 1_000_000_000
    /// </summary>
    public long DefaultStarRatingStd { get; set; }

    /// <summary>
    /// Should / 1_000_000_000
    /// </summary>
    public long DefaultStarRatingTaiko { get; set; }

    /// <summary>
    /// Should / 1_000_000_000
    /// </summary>
    public long DefaultStarRatingCtB { get; set; }

    /// <summary>
    /// Should / 1_000_000_000
    /// </summary>
    public long DefaultStarRatingMania { get; set; }

    //public TimeSpan DrainTime { get; set; }
    public TimeSpan TotalTime { get; set; }
    //public TimeSpan AudioPreviewTime { get; set; }
    public int BeatmapId { get; set; }
    public int BeatmapSetId { get; set; }
    //public DbGameMode GameMode { get; set; }
    [MaxLength(256)] public string Source { get; set; } = null!;
    [MaxLength(1024)] public string Tags { get; set; } = null!;
    [MaxLength(128)] public string FolderName { get; set; } = null!;
    [MaxLength(128)] public string AudioFileName { get; set; } = null!;
    public DateTime UpdateTime { get; set; }
    //public int PlayItemId { get; set; }
    [NotMapped]
    public double StarRating => GameMode switch
    {
        GameMode.Circle => DefaultStarRatingStd,
        GameMode.Taiko => DefaultStarRatingTaiko,
        GameMode.Catch => DefaultStarRatingCtB,
        GameMode.Mania => DefaultStarRatingMania,
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public string AutoArtist => string.IsNullOrWhiteSpace(ArtistUnicode) ? Artist : ArtistUnicode;
    public string AutoTitle => string.IsNullOrWhiteSpace(TitleUnicode) ? Title : TitleUnicode;
}