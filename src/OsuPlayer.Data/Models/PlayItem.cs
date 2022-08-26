using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Coosu.Beatmap.Sections.GamePlay;
using Microsoft.EntityFrameworkCore;

namespace Milki.OsuPlayer.Data.Models;

[Index(nameof(StandardizedPath), IsUnique = true)]
[Index(nameof(StandardizedFolder))]
public sealed class PlayItem : IDisplayablePlayItem
{
    public int Id { get; set; }
    /// <summary>
    /// IsAutoManaged==true: ./...
    /// IsAutoManaged==false: FullPath
    /// </summary>
    [MaxLength(512)]
    public string StandardizedPath { get; set; } = null!;
    [MaxLength(512)]
    public string StandardizedFolder { get; set; } = null!;
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

    public LoosePlayItem ToLoosePlayItem(DateTime playTime, LooseItemType looseItemType)
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
            LooseItemType = looseItemType
        };
    }

    [NotMapped] public string Title => PlayItemDetail.AutoTitle;
    [NotMapped] public string Artist => PlayItemDetail.AutoArtist;
    [NotMapped] public string Creator => PlayItemDetail.Creator;
    [NotMapped] public string Source => PlayItemDetail.Source;
    [NotMapped] public string? ThumbPath => PlayItemAsset?.FullThumbPath;
    [NotMapped] public Dictionary<GameMode, PlayItem[]>? GroupPlayItems => null;
    [NotMapped] public PlayItem CurrentPlayItem => this;
    [NotMapped] public double CanvasLeft { get; set; }
    [NotMapped] public double CanvasTop { get; set; }
    [NotMapped] public int CanvasIndex { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}