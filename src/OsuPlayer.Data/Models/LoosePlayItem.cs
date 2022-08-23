using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public LooseItemType LooseItemType { get; set; }
    [NotMapped]
    public PlayItem? PlayItem { get; set; }

    [NotMapped]
    public bool IsItemLost => PlayItem == null;

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

public enum LooseItemType
{
    RecentPlay,CurrentPlay
}