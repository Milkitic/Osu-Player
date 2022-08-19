namespace Milki.OsuPlayer.Data.Models;

public sealed class MetaComparer : IEqualityComparer<PlayGroupQuery>
{
    private MetaComparer()
    {
    }

    public static MetaComparer Instance { get; } = new();

    public bool Equals(PlayGroupQuery? x, PlayGroupQuery? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return string.Equals(x.Artist, y.Artist, StringComparison.InvariantCulture) &&
               string.Equals(x.ArtistUnicode, y.ArtistUnicode, StringComparison.InvariantCulture) &&
               string.Equals(x.Title, y.Title, StringComparison.InvariantCulture) &&
               string.Equals(x.TitleUnicode, y.TitleUnicode, StringComparison.InvariantCulture) &&
               string.Equals(x.Source, y.Source, StringComparison.InvariantCulture);
    }

    public int GetHashCode(PlayGroupQuery obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Artist, StringComparer.InvariantCulture);
        hashCode.Add(obj.ArtistUnicode, StringComparer.InvariantCulture);
        hashCode.Add(obj.Title, StringComparer.InvariantCulture);
        hashCode.Add(obj.TitleUnicode, StringComparer.InvariantCulture);
        hashCode.Add(obj.Source, StringComparer.InvariantCulture);
        return hashCode.ToHashCode();
    }
}

public sealed class PlayGroupQuery
{
    //public int Id { get; init; }
    //public string Path { get; init; } = null!;
    public string Folder { get; init; } = null!;
    public bool IsAutoManaged { get; init; }
    public string Artist { get; init; } = null!;
    public string ArtistUnicode { get; init; } = null!;
    public string ArtistAuto => string.IsNullOrEmpty(ArtistUnicode) ? Artist : ArtistUnicode;
    public string Title { get; init; } = null!;
    public string TitleUnicode { get; init; } = null!;
    public string TitleAuto => string.IsNullOrEmpty(TitleUnicode) ? Title : TitleUnicode;
    public string Creator { get; init; } = null!;
    //public string Version { get; init; } = null!;
    //public long DefaultStarRatingStd { get; init; }
    //public long DefaultStarRatingTaiko { get; init; }
    //public long DefaultStarRatingCtB { get; init; }
    //public long DefaultStarRatingMania { get; init; }
    //public int BeatmapId { get; init; }
    public int BeatmapSetId { get; init; }
    //public DbGameMode GameMode { get; init; }
    public string Source { get; init; } = null!;
    public string Tags { get; init; } = null!;

    public string? ThumbPath { get; init; }
    public string? VideoPath { get; init; }
    public string? StoryboardVideoPath { get; init; }
    public double StarRating { get; set; }
    public PlayItem PlayItem { get; set; } = null!;
    public PlayItemDetail PlayItemDetail { get; set; } = null!;
}