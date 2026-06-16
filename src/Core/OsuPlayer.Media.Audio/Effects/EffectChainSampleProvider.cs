using System;
using System.Threading;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Owns at most one active <see cref="IDirectXEffectProvider"/> and
/// publishes it as an <see cref="ISampleProvider"/>. When no effect is
/// active the chain becomes a pass-through. All effect swaps are
/// reference-atomic so the audio thread can never observe a half-
/// initialised effect.
/// </summary>
internal sealed class EffectChainSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly EffectParameterSet _parameters;
    private volatile IDirectXEffectProvider? _active;

    public EffectChainSampleProvider(ISampleProvider source, EffectParameterSet parameters)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public DirectXEffectKind ActiveKind { get; private set; } = DirectXEffectKind.None;

    public float ActiveIntensity { get; private set; }

    /// <summary>
    /// Switches the active effect and (re)applies the matching
    /// parameter set. When <paramref name="kind"/> is
    /// <see cref="DirectXEffectKind.None"/> the chain returns to
    /// pass-through. The previous effect is reset to clear delay
    /// lines / envelopes.
    /// </summary>
    public void SetEffect(DirectXEffectKind kind, float intensity)
    {
        var clamped = Math.Clamp(intensity, -1f, 1f);
        ActiveIntensity = clamped;

        if (kind == DirectXEffectKind.None || clamped <= -1f + 0.001f)
        {
            var previous = Interlocked.Exchange(ref _active, null);
            previous?.ResetState();
            ActiveKind = DirectXEffectKind.None;
            return;
        }

        var existing = _active;
        if (existing != null && ActiveKind == kind)
        {
            // Same effect, just a new intensity (send level).
            existing.SetIntensity(clamped);
            return;
        }

        var newEffect = EffectChainBuilder.Create(kind, _source, _parameters, clamped);
        var previous2 = Interlocked.Exchange(ref _active, newEffect);
        previous2?.ResetState();
        ActiveKind = kind;
    }

    /// <summary>
    /// Pushes the parameter subset for the active effect to the
    /// live provider. No-op when no effect is active.
    /// </summary>
    public void ApplyActiveParameters()
    {
        var active = _active;
        if (active == null) return;
        EffectChainBuilder.ApplyParameters(active, ActiveKind, _parameters);
    }

    /// <summary>
    /// Pushes the parameter subset for a specific effect, switching
    /// to that effect first if needed. Useful when the user is
    /// editing a different effect's sliders.
    /// </summary>
    public void ApplyParametersFor(DirectXEffectKind kind)
    {
        if (kind == DirectXEffectKind.None) return;
        var active = _active;
        if (active != null && ActiveKind == kind)
        {
            EffectChainBuilder.ApplyParameters(active, kind, _parameters);
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var active = _active;
        return active != null
            ? active.Read(buffer, offset, count)
            : _source.Read(buffer, offset, count);
    }
}
