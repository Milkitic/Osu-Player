using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Single-LFO flanger with feedback. A flanger is just a chorus with
/// a much shorter delay range and a more aggressive feedback path —
/// that combination creates the characteristic "jet plane" comb
/// filtering sweep.
/// </summary>
internal sealed class FlangerEffectProvider : DirectXEffectProviderBase
{
    private readonly float[] _ringBuffer;
    private readonly int _ringLength;
    private int _writeIndex;
    private float _phase;
    private float _depthSeconds;
    private float _rateHz;
    private float _feedback;
    private float _wet;
    private float _sendAmount;
    private bool _bypass = true;
    private int _sampleRate;
    private int _channels;
    private float[] _dryScratch = Array.Empty<float>();

    public FlangerEffectProvider(ISampleProvider source) : base(source)
    {
        _sampleRate = source.WaveFormat.SampleRate;
        _channels = source.WaveFormat.Channels;
        _ringLength = Math.Max(64, (int)(0.010f * _sampleRate) + 16);
        _ringBuffer = new float[_ringLength];
    }

    public override void SetIntensity(float intensity)
    {
        _sendAmount = WetFromIntensity(intensity);
        _bypass = intensity <= BypassThreshold;
    }

    public void ApplyParameters(FlangerParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        _depthSeconds = p.DepthMs * 0.001f;
        _rateHz = p.RateHz;
        _feedback = Math.Clamp(p.Feedback, -0.95f, 0.95f);
        _wet = Math.Clamp(p.Wet, 0f, 1f);
        _bypass = false;
    }

    public void ResetToDefaults()
    {
        _sendAmount = 0.5f;
        _phase = 0f;
        _bypass = false;
        ApplyParameters(new FlangerParameters());
    }

    public override void ResetState()
    {
        Array.Clear(_ringBuffer, 0, _ringBuffer.Length);
        _writeIndex = 0;
        _phase = 0f;
    }

    protected override void Process(float[] buffer, int offset, int count)
    {
        if (_bypass) return;

        var depthSamples = _depthSeconds * _sampleRate;
        var phaseIncrement = 2f * MathF.PI * _rateHz / _sampleRate;
        var dryAmount = 1f - _sendAmount;

        if (_dryScratch.Length < count) _dryScratch = new float[count];
        Array.Copy(buffer, offset, _dryScratch, 0, count);

        for (var i = 0; i < count; i++)
        {
            var idx = offset + i;
            var input = buffer[idx];

            var mod = MathF.Sin(_phase);
            var delaySamples = depthSamples * (0.5f + 0.5f * mod);
            if (delaySamples < 1f) delaySamples = 1f;
            var intDelay = (int)delaySamples;
            var frac = delaySamples - intDelay;
            var read0 = (_writeIndex - intDelay + _ringLength) % _ringLength;
            var read1 = (read0 - 1 + _ringLength) % _ringLength;
            var delayed = _ringBuffer[read0] * (1f - frac) + _ringBuffer[read1] * frac;

            _ringBuffer[_writeIndex] = input + delayed * _feedback;
            _writeIndex = (_writeIndex + 1) % _ringLength;

            var processed = input * (1f - _wet) + delayed * _wet;
            buffer[idx] = _dryScratch[i] * dryAmount + processed * _sendAmount;

            _phase += phaseIncrement;
            if (_phase > 2f * MathF.PI) _phase -= 2f * MathF.PI;
        }
    }
}
