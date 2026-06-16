using System;

namespace OsuPlayer.Core.Configuration;

/// <summary>
/// Aggregates the parameter set for every DirectSound-style effect.
/// One instance is held in <see cref="EffectsSection.Parameters"/> and
/// is also the value type the runtime effect chain expects when it
/// pushes parameters to a live provider.
/// </summary>
public sealed class EffectParameterSet
{
    public CompressorParameters Compressor { get; set; } = new();
    public ChorusParameters Chorus { get; set; } = new();
    public GargleParameters Gargle { get; set; } = new();
    public ReverbExParameters ReverbEx { get; set; } = new();
    public FlangerParameters Flanger { get; set; } = new();
    public DistortionParameters Distortion { get; set; } = new();

    public object GetParametersFor(DirectXEffectKind kind) => kind switch
    {
        DirectXEffectKind.Compressor => Compressor,
        DirectXEffectKind.Chorus => Chorus,
        DirectXEffectKind.Gargle => Gargle,
        DirectXEffectKind.ReverbEx => ReverbEx,
        DirectXEffectKind.Flanger => Flanger,
        DirectXEffectKind.Distortion => Distortion,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No parameter set for this effect."),
    };

    public void SetParametersFor(DirectXEffectKind kind, object parameters)
    {
        switch (kind)
        {
            case DirectXEffectKind.Compressor when parameters is CompressorParameters c: Compressor = c; break;
            case DirectXEffectKind.Chorus when parameters is ChorusParameters c: Chorus = c; break;
            case DirectXEffectKind.Gargle when parameters is GargleParameters g: Gargle = g; break;
            case DirectXEffectKind.ReverbEx when parameters is ReverbExParameters r: ReverbEx = r; break;
            case DirectXEffectKind.Flanger when parameters is FlangerParameters f: Flanger = f; break;
            case DirectXEffectKind.Distortion when parameters is DistortionParameters d: Distortion = d; break;
            default: throw new ArgumentException($"Parameter type {parameters.GetType().Name} does not match effect {kind}.", nameof(parameters));
        }
    }
}
