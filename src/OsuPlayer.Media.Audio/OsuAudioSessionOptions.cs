namespace Milky.OsuPlayer.Media.Audio;

public sealed class OsuAudioSessionOptions
{
    public string BeatmapFolder { get; init; } = "";
    public string BeatmapFilename { get; init; } = "";
    public string AudioFilename { get; init; } = "";
    public string UserSkinFolder { get; init; } = "";
    public string DefaultHitsoundFolder { get; init; } = "";

    public int ManualOffsetMilliseconds { get; set; }
    public int GeneralOffsetMilliseconds { get; set; }
    public bool EnableNightcoreBeats { get; set; }
    public bool DisableStoryboardSamples { get; set; }

    public float HitsoundVolume { get; set; } = 1;
    public float SampleVolume { get; set; } = 1;
    public float BalanceFactor { get; set; } = 0.35f;
}
