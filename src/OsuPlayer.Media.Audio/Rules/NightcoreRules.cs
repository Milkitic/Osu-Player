using System;

namespace Milky.OsuPlayer.Media.Audio.Rules;

/// <summary>
/// Centralises osu! playback-rate business rules. Previously expressed as
/// raw comparisons (<c>Math.Abs(rate - 1.5f) &lt; 0.001f &amp;&amp; !keepTune</c>)
/// in two separate files; consolidating the predicate here makes the rule
/// explicit, testable, and impossible to drift between call sites.
/// </summary>
public static class NightcoreRules
{
    /// <summary>
    /// The playback rate at which osu! player-style "Nightcore" hitsound
    /// doubling is generated. 1.5x is the convention used by the
    /// <c>DoubleTime</c> / <c>NightCore</c> mods.
    /// </summary>
    public const float NightcoreRate = 1.5f;

    /// <summary>
    /// Tolerance applied when comparing playback rates to
    /// <see cref="NightcoreRate"/>. Necessary because persisted settings
    /// round-trip through JSON and may lose a fraction of a frame.
    /// </summary>
    public const float RateEpsilon = 0.001f;

    /// <summary>
    /// True when the supplied playback configuration should trigger
    /// Nightcore hitsound generation: rate equals
    /// <see cref="NightcoreRate"/> and pitch preservation is disabled.
    /// </summary>
    public static bool ShouldEnableNightcoreBeats(float rate, bool keepTune)
    {
        return Math.Abs(rate - NightcoreRate) < RateEpsilon && !keepTune;
    }
}
