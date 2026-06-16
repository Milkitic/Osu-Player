using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Static factory for the six DirectSound-style effect providers.
/// Lives in one place so <see cref="EffectChainSampleProvider"/> does
/// not have to know about the concrete implementations.
/// </summary>
internal static class EffectChainBuilder
{
    /// <summary>
    /// Creates an effect provider of the requested kind, applies the
    /// matching parameter set, and returns it ready to be plugged
    /// into the chain. Pass <paramref name="intensity"/> for the
    /// master wet/dry send level.
    /// </summary>
    public static IDirectXEffectProvider Create(DirectXEffectKind kind, ISampleProvider source, EffectParameterSet parameters, float intensity)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(parameters);

        var provider = CreateRaw(kind, source);
        ApplyParameters(provider, kind, parameters);
        provider.SetIntensity(intensity);
        return provider;
    }

    /// <summary>
    /// Pushes the matching parameter subset of <paramref name="parameters"/>
    /// to an already-constructed provider. Used when the user
    /// changes one slider and we want to update the live effect
    /// without re-creating it.
    /// </summary>
    public static void ApplyParameters(IDirectXEffectProvider provider, DirectXEffectKind kind, EffectParameterSet parameters)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(parameters);

        switch (kind)
        {
            case DirectXEffectKind.Compressor when provider is CompressorEffectProvider c: c.ApplyParameters(parameters.Compressor); break;
            case DirectXEffectKind.Chorus when provider is ChorusEffectProvider c: c.ApplyParameters(parameters.Chorus); break;
            case DirectXEffectKind.Gargle when provider is GargleEffectProvider g: g.ApplyParameters(parameters.Gargle); break;
            case DirectXEffectKind.ReverbEx when provider is ReverbExEffectProvider r: r.ApplyParameters(parameters.ReverbEx); break;
            case DirectXEffectKind.Flanger when provider is FlangerEffectProvider f: f.ApplyParameters(parameters.Flanger); break;
            case DirectXEffectKind.Distortion when provider is DistortionEffectProvider d: d.ApplyParameters(parameters.Distortion); break;
            default: throw new ArgumentException($"Provider does not match kind {kind}.", nameof(provider));
        }
    }

    private static IDirectXEffectProvider CreateRaw(DirectXEffectKind kind, ISampleProvider source) => kind switch
    {
        DirectXEffectKind.Compressor => new CompressorEffectProvider(source),
        DirectXEffectKind.Chorus => new ChorusEffectProvider(source),
        DirectXEffectKind.Gargle => new GargleEffectProvider(source),
        DirectXEffectKind.ReverbEx => new ReverbExEffectProvider(source),
        DirectXEffectKind.Flanger => new FlangerEffectProvider(source),
        DirectXEffectKind.Distortion => new DistortionEffectProvider(source),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Cannot create effect provider for this kind."),
    };
}
