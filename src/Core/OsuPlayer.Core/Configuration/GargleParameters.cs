using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Parameters for the Gargle effect — amplitude modulation. The
/// <see cref="Waveform"/> selector lets the operator switch between
/// the two shapes the original DirectSound gargle supported.
/// </summary>
public enum GargleWaveform
{
    Triangle = 0,
    Square = 1,
}

public sealed class GargleParameters
{
    /// <summary>Modulation rate in Hz.</summary>
    public float RateHz { get; set; } = 5f;

    /// <summary>Modulation depth in <c>[0, 1]</c>. 0 = no modulation, 1 = full tremolo.</summary>
    public float Depth { get; set; } = 0.5f;

    public GargleWaveform Waveform { get; set; } = GargleWaveform.Triangle;

    public GargleParameters Clone() => new()
    {
        RateHz = RateHz,
        Depth = Depth,
        Waveform = Waveform,
    };
}
