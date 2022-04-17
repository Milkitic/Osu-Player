namespace OsuPlayer.Data.Models;

public sealed class PlayListRelation
{
    public int Id { get; set; }
    public PlayList Collection { get; set; } = null!;
    public int CollectionId { get; set; }
    public PlayItem PlayItem { get; set; } = null!;
    public int PlayItemId { get; set; }
}