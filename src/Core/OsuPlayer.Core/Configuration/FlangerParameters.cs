using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Parameters for the Flanger effect. Like chorus, but with a much
/// shorter modulation range and an aggressive feedback path that
/// produces the characteristic "jet plane" sweep.
/// </summary>
public sealed class FlangerParameters
{
    /// <summary>Modulation depth in milliseconds.</summary>
    public float DepthMs { get; set; } = 1.2f;

    /// <summary>LFO rate in Hz.</summary>
    public float RateHz { get; set; } = 0.4f;

    /// <summary>Feedback gain in <c>[-0.95, 0.95]</c>. Positive = metallic, negative = hollow.</summary>
    public float Feedback { get; set; } = 0.3f;

    /// <summary>Wet/dry mix in <c>[0, 1]</c>.</summary>
    public float Wet { get; set; } = 0.5f;

    public FlangerParameters Clone() => new()
    {
        DepthMs = DepthMs,
        RateHz = RateHz,
        Feedback = Feedback,
        Wet = Wet,
    };
}
