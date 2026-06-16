using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Parameters for the Compressor effect. Defaults are tuned for a
/// general-purpose "glue" compressor on a hitsound bus — enough to
/// tame loud claps without colouring the sound at moderate settings.
/// </summary>
public sealed class CompressorParameters
{
    /// <summary>Threshold in dB FS above which compression engages.</summary>
    public float ThresholdDb { get; set; } = -18f;

    /// <summary>Compression ratio above the threshold. 1 = no compression.</summary>
    public float Ratio { get; set; } = 4f;

    /// <summary>Attack time in milliseconds. Lower = faster response.</summary>
    public float AttackMs { get; set; } = 5f;

    /// <summary>Release time in milliseconds. Higher = slower recovery.</summary>
    public float ReleaseMs { get; set; } = 100f;

    /// <summary>Makeup gain in dB applied after compression.</summary>
    public float MakeupDb { get; set; } = 0f;

    public CompressorParameters Clone() => new()
    {
        ThresholdDb = ThresholdDb,
        Ratio = Ratio,
        AttackMs = AttackMs,
        ReleaseMs = ReleaseMs,
        MakeupDb = MakeupDb,
    };
}
