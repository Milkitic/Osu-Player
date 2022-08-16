#nullable enable
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class BindKeySection
{
    public BindKeys? TogglePlay { get; set; }
    public BindKeys? PrevSong { get; set; }
    public BindKeys? NextSong { get; set; }
    public BindKeys? VolumeUp { get; set; }
    public BindKeys? VolumeDown { get; set; }
    public BindKeys? SwitchFullMiniMode { get; set; }
    public BindKeys? AddCurrentToFav { get; set; }
    public BindKeys? SwitchLyricWindow { get; set; }
}