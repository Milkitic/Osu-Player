using Milki.Extensions.MixPlayer.Devices;

namespace OsuPlayer.Shared.Configuration;

public sealed class SectionPlay
{
    private float _playbackRate = 1;
    public int GeneralOffset { get; set; }
    public float MainVolume { get; set; }
    public float MusicVolume { get; set; }
    public float AdditionVolume { get; set; }
    public float SampleVolume { get; set; }
    public float Balance { get; set; }

    public float PlaybackRate
    {
        get
        {
            if (_playbackRate < 0.1) return 0.1f;
            if (_playbackRate > 10) return 10;
            return _playbackRate;
        }
        set => _playbackRate = value;
    }

    public bool IsTuneKept { get; set; } = true;
    public DeviceDescription? DeviceDescription { get; set; }
}