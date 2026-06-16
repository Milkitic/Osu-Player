using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Parameters for the Distortion effect. The pre-gain drives the
/// tanh soft clipper, the cutoff shapes the post-distortion tone.
/// </summary>
public sealed class DistortionParameters
{
    /// <summary>Pre-gain in dB. Drives the tanh soft clipper.</summary>
    public float GainDb { get; set; } = 12f;

    /// <summary>Post-distortion low-pass cutoff in Hz. Lower = darker.</summary>
    public float CutoffHz { get; set; } = 4000f;

    public DistortionParameters Clone() => new()
    {
        GainDb = GainDb,
        CutoffHz = CutoffHz,
    };
}
