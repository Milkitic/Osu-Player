using System.ComponentModel;
using Coosu.Beatmap.Sections.GamePlay;

namespace Milki.OsuPlayer.Data.Models;

public interface IDisplayablePlayItem : INotifyPropertyChanged
{
    string AutoTitle { get; }
    string AutoArtist { get; }
    string Creator { get; }
    string Source { get; }
    string? ThumbPath { get; set; }
    Dictionary<GameMode, PlayItem[]>? GroupPlayItems { get; }
    PlayItem CurrentPlayItem { get; }
    double CanvasLeft { get; set; }
    double CanvasTop { get; set; }
    int CanvasIndex { get; set; }
}