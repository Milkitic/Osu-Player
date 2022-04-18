using Milki.Extensions.MixPlayer.Devices;

namespace OsuPlayer.Audio;

public sealed class PlayerOptions
{
    public PlayerOptions(string defaultFolder)
    {
        DefaultFolder = defaultFolder;
    }

    public string DefaultFolder { get; }
    public DeviceDescription? DeviceDescription { get; set; }
    public float InitialPlaybackRate { get; set; }
    public bool InitialKeepTune { get; set; }
    public int InitialOffset { get; set; }

    public float InitialMainVolume { get; set; } = 1;
    public float InitialMusicVolume { get; set; } = 1;
    public float InitialHitsoundVolume { get; set; } = 1;
    public float InitialSampleVolume { get; set; } = 1;
    public float InitialHitsoundBalance { get; set; }
}