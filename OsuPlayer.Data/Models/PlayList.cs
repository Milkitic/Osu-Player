using Microsoft.EntityFrameworkCore;

namespace OsuPlayer.Data.Models;

[Index(nameof(Index))]
public sealed class PlayList : IAutoCreatable, IAutoUpdatable
{
    public int Id { get; set; }
    public bool IsDefault { get; set; }
    public string Name { get; set; } = null!;
    public int Index { get; set; }
    public string? ImagePath { get; set; }
    public string? Description { get; set; }

    public List<PlayItem> PlayItems { get; set; } = null!;
    public List<PlayListPlayItemRelation> PlayListRelations { get; set; }

    public DateTime CreateTime { get; set; }
    public DateTime UpdatedTime { get; set; }
}