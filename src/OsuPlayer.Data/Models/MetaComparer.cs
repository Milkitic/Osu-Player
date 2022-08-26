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