#nullable enable

using Milki.Extensions.MixPlayer.Devices;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class PlaySection
{
    public int GeneralOffset { get; set; }
    public bool ReplacePlayList { get; set; } = true;
    public float PlaybackRate { get; set; } = 1;
    public bool PlayUseTempo { get; set; }
    public bool AutoPlay { get; set; } = false;
    //public bool Memory { get; set; } = true;
    public DeviceDescription? DeviceInfo { get; set; }
    public int DesiredLatency { get; set; } = 5;
    public bool IsExclusive { get; set; } = false;
    public PlaylistMode PlayListMode { get; set; } = PlaylistMode.Normal;
}