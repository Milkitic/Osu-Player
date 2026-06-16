using KeyAsio.Core.Audio.SampleProviders.BalancePans;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Mutable, session-level audio configuration. Path-resolution lives in
/// <see cref="BeatmapResources"/>; this type only owns the values that
/// change during a session (volumes, offsets, mods).
/// </summary>
public sealed class OsuAudioSessionOptions
{
    /// <summary>
    /// Resolved on-disk resources the session should read from.
    /// </summary>
    public required BeatmapResources Resources { get; init; }

    public int ManualOffsetMilliseconds { get; set; }
    public int GeneralOffsetMilliseconds { get; set; }
    public bool EnableNightcoreBeats { get; set; }
    public bool DisableStoryboardSamples { get; set; }

    public float HitsoundVolume { get; set; } = 1;
    public float SampleVolume { get; set; } = 1;
    public float BalanceFactor { get; set; } = 0.35f;
    public BalanceMode BalanceMode { get; set; } = BalanceMode.MidSide;

    /// <summary>
    /// DirectSound-style effect selection (kind, master intensity,
    /// and per-bus toggles). Defaults to disabled.
    /// </summary>
    public DirectXEffectSettings Effects { get; set; } = DirectXEffectSettings.Disabled;
}
