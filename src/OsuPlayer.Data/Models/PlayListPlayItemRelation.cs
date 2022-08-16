using System;

namespace Milki.OsuPlayer.Data.Models;

public sealed class PlayListPlayItemRelation : IAutoCreatable
{
    public int Index { get; set; }
    public PlayList PlayList { get; set; } = null!;
    public int PlayListId { get; set; }
    public PlayItem PlayItem { get; set; } = null!;
    public int PlayItemId { get; set; }
    public DateTime CreateTime { get; set; }
}