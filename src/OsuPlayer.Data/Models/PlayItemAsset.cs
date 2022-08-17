using System.ComponentModel.DataAnnotations.Schema;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.Data.Models;

public sealed class PlayItemAsset : VmBase
{
    private string? _fullThumbPath;

    public int Id { get; set; }
    public string? ThumbPath { get; set; }
    public string? VideoPath { get; set; }
    public string? StoryboardVideoPath { get; set; }

    [NotMapped]
    public string? FullThumbPath
    {
        get => _fullThumbPath;
        set => this.RaiseAndSetIfChanged(ref _fullThumbPath, value);
    }

    //public int PlayItemId { get; set; }
}