using Milki.Extensions.MixPlayer.Devices;

namespace OsuPlayer.Shared.Configuration;

public sealed class SectionPlay
{
    public int GeneralOffset { get; set; }
    public float MainVolume { get; set; }
    public float MusicVolume { get; set; }
    public float AdditionVolume { get; set; }
    public float SampleVolume { get; set; }
    public float Balance { get; set; }
    public float PlaybackRate { get; set; }
    public bool IsTuneKept { get; set; }
    public DeviceDescription? DeviceDescription { get; set; }
}