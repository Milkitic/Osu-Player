using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Milki.OsuPlayer.Data.Models;

[Index(nameof(Index))]
[Index(nameof(LastPlay))]
public sealed class LoosePlayItem
{
    public int Id { get; set; }
    public int Index { get; set; }
    public DateTime LastPlay { get; set; }

    [MaxLength(256)]
    public string Title { get; set; } = null!;
    [MaxLength(256)]
    public string Artist { get; set; } = null!;
    [MaxLength(64)]
    public string Creator { get; set; } = null!;
    [MaxLength(128)]
    public string Version { get; set; } = null!;

    public int? PlayItemId { get; set; }

    public void UpdateFromPlayItem(PlayItem playItem, DateTime playTime)
    {
        Artist = playItem.PlayItemDetail.Artist;
        Creator = playItem.PlayItemDetail.Creator;
        Title = playItem.PlayItemDetail.Title;
        LastPlay = playTime;
        PlayItemId = playItem.Id;
        //PlayItemPath = playItem.Path;
        Version = playItem.PlayItemDetail.Version;
    }
}