using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Parameters for the Reverb Ex effect (Freeverb-style). All values
/// are in <c>[0, 1]</c> and correspond to the standard Freeverb
/// parameter set; <see cref="Wet1"/> and <see cref="Wet2"/> are the
/// per-channel wet levels used for stereo width blending.
/// </summary>
public sealed class ReverbExParameters
{
    /// <summary>Room size in <c>[0, 1]</c>. Higher = longer decay.</summary>
    public float RoomSize { get; set; } = 0.7f;

    /// <summary>Damping in <c>[0, 1]</c>. Higher = darker tail.</summary>
    public float Damp { get; set; } = 0.4f;

    public float Wet1 { get; set; } = 0.33f;
    public float Wet2 { get; set; } = 0.33f;
    public float Dry { get; set; } = 0.5f;

    /// <summary>Stereo width in <c>[0, 1]</c>. 1 = full stereo separation.</summary>
    public float Width { get; set; } = 0.8f;

    public ReverbExParameters Clone() => new()
    {
        RoomSize = RoomSize,
        Damp = Damp,
        Wet1 = Wet1,
        Wet2 = Wet2,
        Dry = Dry,
        Width = Width,
    };
}
