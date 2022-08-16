using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Milki.OsuPlayer.Data.Models;

[Index(nameof(Path), IsUnique = true)]
[Index(nameof(Folder))]
public sealed class PlayItem
{
    public int Id { get; set; }
    /// <summary>
    /// IsAutoManaged==true: ./...
    /// IsAutoManaged==false: FullPath
    /// </summary>
    [MaxLength(512)]
    public string Path { get; set; } = null!;
    [MaxLength(512)]
    public string Folder { get; set; } = null!;
    public bool IsAutoManaged { get; set; }
    public PlayItemDetail PlayItemDetail { get; set; } = null!;
    public int PlayItemDetailId { get; set; }
    public PlayItemConfig? PlayItemConfig { get; set; }
    public int? PlayItemConfigId { get; set; }
    public PlayItemAsset? PlayItemAsset { get; set; }
    public int? PlayItemAssetId { get; set; }
    public DateTime? LastPlay { get; set; }

    public List<PlayList> PlayLists { get; set; }
    public List<PlayListPlayItemRelation> PlayListRelations { get; set; }

    public LoosePlayItem ToLoosePlayItem(DateTime playTime)
    {
        return new LoosePlayItem
        {
            Artist = string.IsNullOrEmpty(PlayItemDetail.ArtistUnicode)
                ? PlayItemDetail.Artist
                : PlayItemDetail.ArtistUnicode,
            Creator = PlayItemDetail.Creator,
            Title = string.IsNullOrEmpty(PlayItemDetail.TitleUnicode)
                ? PlayItemDetail.Title
                : PlayItemDetail.TitleUnicode,
            LastPlay = playTime,
            PlayItemId = Id,
            Version = PlayItemDetail.Version,
        };
    }
}