namespace OsuPlayer.Data.Models;

public sealed class PlayListPlayItemRelation
{
    //public int Id { get; set; }
    public PlayList PlayList { get; set; } = null!;
    public int PlayListId { get; set; }
    public PlayItem PlayItem { get; set; } = null!;
    public int PlayItemId { get; set; }
}