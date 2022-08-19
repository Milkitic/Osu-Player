using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milki.OsuPlayer.Data.Models;

public sealed class ExportItem
{
    public int Id { get; set; }
    public long Size { get; set; }
    [MaxLength(512)]
    public string? ExportPath { get; set; }
    public DateTime ExportTime { get; set; }

    [MaxLength(256)]
    public string Title { get; set; } = null!;
    [MaxLength(256)]
    public string Artist { get; set; } = null!;
    [MaxLength(64)]
    public string Creator { get; set; } = null!;
    [MaxLength(128)]
    public string Version { get; set; } = null!;

    [NotMapped]
    public bool IsItemLost => PlayItem == null;

    public int? PlayItemId { get; set; }
    [NotMapped]
    public PlayItem? PlayItem { get; set; }
    [MaxLength(512)]
    public string? PlayItemStandardizedPath { get; set; }
}