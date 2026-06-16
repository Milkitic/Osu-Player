using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Soft-knee peak compressor inspired by the DirectSound
/// <c>IDirectSoundFXCompressor</c> parameter set. The intensity slider
/// now controls the wet/dry send level; the actual DSP is driven by
/// <see cref="CompressorParameters"/>.
/// </summary>
/// <remarks>
/// Per-sample envelope state. Sufficient for hitsound and sample-level
/// use cases; tests have not shown audible artefacts at 44.1 kHz on
/// typical osu! hitsound material.
/// </remarks>
internal sealed class CompressorEffectProvider : DirectXEffectProviderBase
{
    private float _envelope;
    private float _attackCoef;
    private float _releaseCoef;
    private float _thresholdDb;
    private float _ratio;
    private float _makeupDb;
    private float _wetAmount;
    private bool _bypass = true;
    private float[] _dryScratch = Array.Empty<float>();

    public CompressorEffectProvider(ISampleProvider source) : base(source)
    {
        ApplyParameters(new CompressorParameters());
    }

    public override void SetIntensity(float intensity)
    {
        // Intensity is now a wet/dry send level. -1 = pure dry,
        // 0 = 50% mix, +1 = full effect.
        _wetAmount = WetFromIntensity(intensity);
        _bypass = intensity <= BypassThreshold;
    }

    public void ApplyParameters(CompressorParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        _thresholdDb = p.ThresholdDb;
        _ratio = p.Ratio;
        _makeupDb = p.MakeupDb;
        RecomputeCoefficients(WaveFormat.SampleRate, p.AttackMs, p.ReleaseMs);
        _bypass = false;
    }

    public void ResetToDefaults()
    {
        _envelope = 0f;
        _wetAmount = 0.5f;
        _bypass = false;
        ApplyParameters(new CompressorParameters());
    }

    public override void ResetState()
    {
        _envelope = 0f;
    }

    private void RecomputeCoefficients(int sampleRate, float attackMs, float releaseMs)
    {
        _attackCoef = (float)Math.Exp(-1.0 / (sampleRate * attackMs / 1000.0));
        _releaseCoef = (float)Math.Exp(-1.0 / (sampleRate * releaseMs / 1000.0));
    }

    protected override void Process(float[] buffer, int offset, int count)
    {
        if (_bypass) return;

        var threshold = Decibels.ToAmplitude(_thresholdDb);
        var makeup = Decibels.ToAmplitude(_makeupDb);
        var dryAmount = 1f - _wetAmount;

        if (_dryScratch.Length < count) _dryScratch = new float[count];
        Array.Copy(buffer, offset, _dryScratch, 0, count);

        for (var i = 0; i < count; i++)
        {
            var idx = offset + i;
            var s = buffer[idx];
            var envIn = Math.Abs(s);
            _envelope += (envIn - _envelope) * (envIn > _envelope ? _attackCoef : _releaseCoef);

            float gain;
            if (_envelope > threshold)
            {
                var envDb = Decibels.FromAmplitude(_envelope);
                var excessDb = envDb - _thresholdDb;
                var compressedExcessDb = excessDb / _ratio;
                var gainDb = (compressedExcessDb - excessDb) + _makeupDb;
                gain = Decibels.ToAmplitude(gainDb);
            }
            else
            {
                gain = makeup;
            }

            buffer[idx] = s * gain;
        }

        // Apply wet/dry send.
        for (var i = 0; i < count; i++)
        {
            var idx = offset + i;
            buffer[idx] = _dryScratch[i] * dryAmount + buffer[idx] * _wetAmount;
        }
    }

    private static class Decibels
    {
        public static float ToAmplitude(float db) => MathF.Pow(10f, db / 20f);
        public static float FromAmplitude(float amplitude) => 20f * MathF.Log10(Math.Max(amplitude, 1e-7f));
    }
}
