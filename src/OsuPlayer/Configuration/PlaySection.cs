#nullable enable

using Milki.Extensions.MixPlayer.Devices;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class PlaySection
{
    public bool IsAutoPlayOnStartup { get; set; } = false;
    public bool IsReplacePlayList { get; set; } = true;
    public int PlayerDesiredLatency { get; set; } = 5;
    public DeviceDescription? PlayerDeviceInfo { get; set; }
    public int PlayerGeneralOffset { get; set; }
    public bool PlayerIsExclusive { get; set; } = false;
    public bool PlayerKeepTune { get; set; }
    public float PlayerPlaybackRate { get; set; } = 1;
    public PlayListMode PlayListMode { get; set; } = PlayListMode.Normal;
}