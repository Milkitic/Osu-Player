using Microsoft.EntityFrameworkCore;

namespace OsuPlayer.Data.Models;

[Index(nameof(Path), IsUnique = true)]
public sealed class PlayItem
{
    public int Id { get; set; }
    /// <summary>
    /// IsAutoManaged==true: ./...
    /// IsAutoManaged==false: FullPath
    /// </summary>
    public string Path { get; set; } = null!;
    public bool IsAutoManaged { get; set; }
    public PlayItemDetail PlayItemDetail { get; set; } = null!;
    public int CachedInfoId { get; set; }
}