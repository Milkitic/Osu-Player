using Coosu.Database.DataTypes;

namespace OsuPlayer.Data.Models;

public class PlayItemQuery
{
    public PlayItemQuery()
    {
        IsAutoManaged = false;
        DefaultStarRatingStd = 0;
        DefaultStarRatingTaiko = 0;
        DefaultStarRatingCtB = 0;
        DefaultStarRatingMania = 0;
        BeatmapId = 0;
        BeatmapSetId = 0;
        GameMode = DbGameMode.Circle;
    }

    public string Path { get; init; } = null!;
    public bool IsAutoManaged { get; init; }
    public string Artist { get; init; } = null!;
    public string ArtistUnicode { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string TitleUnicode { get; init; } = null!;
    public string Creator { get; init; } = null!;
    public string Version { get; init; } = null!;
    public long DefaultStarRatingStd { get; init; }
    public long DefaultStarRatingTaiko { get; init; }
    public long DefaultStarRatingCtB { get; init; }
    public long DefaultStarRatingMania { get; init; }
    public int BeatmapId { get; init; }
    public int BeatmapSetId { get; init; }
    public DbGameMode GameMode { get; init; }
    public string Source { get; init; } = null!;
    public string Tags { get; init; } = null!;
}