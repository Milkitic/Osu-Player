using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Parameters for the Chorus effect. Three voices are each driven by
/// the same LFO with a 120° phase offset; the three "Voice N Delay"
/// properties set the base delay of each voice before modulation.
/// </summary>
public sealed class ChorusParameters
{
    public float Voice1DelayMs { get; set; } = 12f;
    public float Voice2DelayMs { get; set; } = 18f;
    public float Voice3DelayMs { get; set; } = 25f;

    /// <summary>Modulation depth in milliseconds.</summary>
    public float DepthMs { get; set; } = 5f;

    /// <summary>LFO rate in Hz.</summary>
    public float RateHz { get; set; } = 1.2f;

    /// <summary>Wet/dry mix in <c>[0, 1]</c>.</summary>
    public float Wet { get; set; } = 0.4f;

    public ChorusParameters Clone() => new()
    {
        Voice1DelayMs = Voice1DelayMs,
        Voice2DelayMs = Voice2DelayMs,
        Voice3DelayMs = Voice3DelayMs,
        DepthMs = DepthMs,
        RateHz = RateHz,
        Wet = Wet,
    };
}
