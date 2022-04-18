namespace OsuPlayer.Data.Models;

public sealed class PlayItemConfig
{
    public int Id { get; set; }
    public int Offset { get; set; }
    public int LyricOffset { get; set; }
    //[MaxLength(512)]
    //public string? ForceLyricId { get; set; }
}