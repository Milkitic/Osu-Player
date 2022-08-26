using Coosu.Beatmap.Sections.GamePlay;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.Data.Models;

public sealed class PlayGroupQuery : VmBase, IDisplayablePlayItem
{
    private string? _thumbPath;

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

    //public string? ThumbPath { get; init; }
    //public string? VideoPath { get; init; }
    //public string? StoryboardVideoPath { get; init; }
    public double StarRating { get; set; }
    public PlayItem CurrentPlayItem { get; set; } = null!;
    public double CanvasLeft { get; set; }
    public double CanvasTop { get; set; }
    public int CanvasIndex { get; set; }
    public PlayItemDetail CurrentPlayItemDetail { get; set; } = null!;
    public Dictionary<GameMode, PlayItem[]>? GroupPlayItems { get; set; }

    public string? ThumbPath
    {
        get => _thumbPath;
        set => this.RaiseAndSetIfChanged(ref _thumbPath, value);
    }
}