using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Boilerplate for in-place sample effects. Mirrors the shape of
/// KeyAsio's <c>LimiterBase</c>: read from <see cref="Source"/> into the
/// caller's buffer, then run <see cref="Process"/> on the populated
/// region. Concrete effects stay focused on their DSP.
/// </summary>
internal abstract class DirectXEffectProviderBase : IDirectXEffectProvider
{
    protected ISampleProvider Source { get; }

    protected DirectXEffectProviderBase(ISampleProvider source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public WaveFormat WaveFormat => Source.WaveFormat;

    public virtual int Read(float[] buffer, int offset, int count)
    {
        var read = Source.Read(buffer, offset, count);
        if (read == 0) return 0;
        Process(buffer, offset, read);
        return read;
    }

    public abstract void SetIntensity(float intensity);

    public virtual void ResetState()
    {
    }

    /// <summary>
    /// Transforms samples already written to <paramref name="buffer"/> in
    /// the range <c>[offset, offset + count)</c>. Implementations must
    /// edit in place — the buffer is not copied.
    /// </summary>
    protected abstract void Process(float[] buffer, int offset, int count);

    protected static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// Maps an intensity in <c>[-1, +1]</c> to a <c>[0, 1]</c> parameter
    /// range. Values at or below <c>-1</c> are clamped to 0; values at
    /// or above <c>+1</c> are clamped to 1. Callers that want bypass
    /// semantics should compare the raw intensity against
    /// <see cref="BypassThreshold"/> before calling this.
    /// </summary>
    protected static float NormaliseIntensity(float intensity)
    {
        if (intensity <= -1f) return 0f;
        if (intensity >= 1f) return 1f;
        return (intensity + 1f) * 0.5f;
    }

    /// <summary>
    /// Maps a master intensity value in <c>[-1, +1]</c> to a wet/dry
    /// send amount in <c>[0, 1]</c>. At <c>-1</c> the effect is
    /// fully bypassed (pure dry), at <c>+1</c> the effect is fully
    /// audible (pure wet). Every effect in this module uses the same
    /// curve so the UI behaves consistently across kinds.
    /// </summary>
    public static float WetFromIntensity(float intensity)
    {
        if (intensity <= BypassThreshold) return 0f;
        if (intensity >= 1f) return 1f;
        return (intensity + 1f) * 0.5f;
    }

    protected const float BypassThreshold = -0.999f;
}
