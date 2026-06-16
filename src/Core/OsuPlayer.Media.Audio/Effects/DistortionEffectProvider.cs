using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Soft-clip distortion. A pre-gain pushes samples into the
/// hyperbolic-tangent saturator and a one-pole lowpass simulates a
/// tone control. The intensity slider now controls the master
/// wet/dry send level; gain and cutoff come from
/// <see cref="DistortionParameters"/>.
/// </summary>
internal sealed class DistortionEffectProvider : DirectXEffectProviderBase
{
    private float _gainLinear = 1f;
    private float _cutoffHz = 8000f;
    private float _lpCoef;
    private float _lpState;
    private float _sendAmount;
    private int _sampleRate;
    private bool _bypass = true;
    private float[] _dryScratch = Array.Empty<float>();

    public DistortionEffectProvider(ISampleProvider source) : base(source)
    {
        _sampleRate = source.WaveFormat.SampleRate;
        UpdateLowpassCoefficient();
    }

    public override void SetIntensity(float intensity)
    {
        _sendAmount = WetFromIntensity(intensity);
        _bypass = intensity <= BypassThreshold;
    }

    public void ApplyParameters(DistortionParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        _gainLinear = MathF.Pow(10f, p.GainDb / 20f);
        _cutoffHz = p.CutoffHz;
        UpdateLowpassCoefficient();
        _bypass = false;
    }

    public void ResetToDefaults()
    {
        _sendAmount = 0.5f;
        _lpState = 0f;
        _bypass = false;
        ApplyParameters(new DistortionParameters());
    }

    public override void ResetState()
    {
        _lpState = 0f;
    }

    private void UpdateLowpassCoefficient()
    {
        if (_sampleRate <= 0) { _lpCoef = 1f; return; }
        var rc = 1f / (2f * MathF.PI * _cutoffHz);
        _lpCoef = 1f / (rc * _sampleRate + 1f);
    }

    protected override void Process(float[] buffer, int offset, int count)
    {
        if (_bypass) return;

        var dryAmount = 1f - _sendAmount;
        if (_dryScratch.Length < count) _dryScratch = new float[count];
        Array.Copy(buffer, offset, _dryScratch, 0, count);

        for (var i = 0; i < count; i++)
        {
            var idx = offset + i;
            var driven = buffer[idx] * _gainLinear;
            var clipped = MathF.Tanh(driven);
            _lpState += _lpCoef * (clipped - _lpState);
            buffer[idx] = _dryScratch[i] * dryAmount + _lpState * _sendAmount;
        }
    }
}
