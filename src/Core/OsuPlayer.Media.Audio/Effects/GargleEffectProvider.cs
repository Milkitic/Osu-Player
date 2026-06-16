using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Amplitude-modulation "gargle" — the classic "trucker" effect.
/// Supports both triangle and square waveforms (the two shapes the
/// original DirectSound gargle exposed).
/// </summary>
internal sealed class GargleEffectProvider : DirectXEffectProviderBase
{
    private float _phase;
    private float _rateHz;
    private float _depth;
    private GargleWaveform _waveform = GargleWaveform.Triangle;
    private float _sendAmount;
    private bool _bypass = true;
    private float[] _dryScratch = Array.Empty<float>();

    public GargleEffectProvider(ISampleProvider source) : base(source)
    {
    }

    public override void SetIntensity(float intensity)
    {
        _sendAmount = WetFromIntensity(intensity);
        _bypass = intensity <= BypassThreshold;
    }

    public void ApplyParameters(GargleParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        _rateHz = p.RateHz;
        _depth = Math.Clamp(p.Depth, 0f, 1f);
        _waveform = p.Waveform;
        _bypass = false;
    }

    public void ResetToDefaults()
    {
        _sendAmount = 0.5f;
        _phase = 0f;
        _bypass = false;
        ApplyParameters(new GargleParameters());
    }

    public override void ResetState()
    {
        _phase = 0f;
    }

    protected override void Process(float[] buffer, int offset, int count)
    {
        if (_bypass) return;

        var sampleRate = WaveFormat.SampleRate;
        var phaseIncrement = _rateHz / sampleRate;
        var minGain = 1f - _depth;
        var dryAmount = 1f - _sendAmount;

        if (_dryScratch.Length < count) _dryScratch = new float[count];
        Array.Copy(buffer, offset, _dryScratch, 0, count);

        for (var i = 0; i < count; i++)
        {
            var idx = offset + i;
            _phase += phaseIncrement;
            if (_phase >= 1f) _phase -= 1f;

            float gain;
            if (_waveform == GargleWaveform.Square)
            {
                gain = _phase < 0.5f ? 1f : minGain;
            }
            else
            {
                var tri = _phase < 0.5f ? _phase * 2f : (1f - _phase) * 2f;
                gain = minGain + _depth * tri;
            }

            buffer[idx] = _dryScratch[i] * dryAmount + buffer[idx] * gain * _sendAmount;
        }
    }
}
