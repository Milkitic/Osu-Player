using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// Three-voice chorus. Each voice shares a global LFO but uses a 120°
/// phase offset to avoid correlated modulation, the hallmark of a
/// "thick" chorus. Per-channel ring buffers keep stereo material in
/// stereo.
/// </summary>
internal sealed class ChorusEffectProvider : DirectXEffectProviderBase
{
    private const int VoiceCount = 3;

    private readonly float[][] _ringBuffers; // [channel][sample]
    private readonly int[] _ringLengths;     // samples per channel
    private readonly int[] _baseOffsets;    // base delay in samples per voice
    private readonly int[] _writeIndices;   // per channel
    private readonly float[] _phases;       // per voice LFO phase
    private float _depthSeconds;
    private float _rateHz;
    private float _wet;
    private float _sendAmount;
    private bool _bypass = true;
    private int _sampleRate;
    private int _channels;
    private float[] _dryScratch = Array.Empty<float>();

    public ChorusEffectProvider(ISampleProvider source) : base(source)
    {
        _sampleRate = source.WaveFormat.SampleRate;
        _channels = source.WaveFormat.Channels;
        _ringBuffers = new float[_channels][];
        _ringLengths = new int[_channels];
        _writeIndices = new int[_channels];
        _phases = new float[VoiceCount];

        _baseOffsets = new int[VoiceCount];
        _sendAmount = 0.5f;
        ApplyParameters(new ChorusParameters());
    }

    public override void SetIntensity(float intensity)
    {
        _sendAmount = WetFromIntensity(intensity);
        _bypass = intensity <= BypassThreshold;
    }

    public void ApplyParameters(ChorusParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        _baseOffsets[0] = Math.Max(1, (int)(p.Voice1DelayMs * 0.001f * _sampleRate));
        _baseOffsets[1] = Math.Max(1, (int)(p.Voice2DelayMs * 0.001f * _sampleRate));
        _baseOffsets[2] = Math.Max(1, (int)(p.Voice3DelayMs * 0.001f * _sampleRate));
        _depthSeconds = p.DepthMs * 0.001f;
        _rateHz = p.RateHz;
        _wet = Math.Clamp(p.Wet, 0f, 1f);
        _bypass = false;

        // The ring buffer is sized to the longest base delay plus the
        // current modulation depth. Re-allocate whenever the depth
        // exceeds the previous buffer length.
        var maxBaseMs = Math.Max(p.Voice1DelayMs, Math.Max(p.Voice2DelayMs, p.Voice3DelayMs));
        var required = (int)((maxBaseMs + p.DepthMs) * 0.001f * _sampleRate) + 16;
        for (var c = 0; c < _channels; c++)
        {
            if (_ringBuffers[c] == null || _ringLengths[c] < required)
            {
                _ringBuffers[c] = new float[required];
                _ringLengths[c] = required;
                _writeIndices[c] = 0;
            }
        }
    }

    public void ResetToDefaults()
    {
        _sendAmount = 0.5f;
        _bypass = false;
        Array.Clear(_phases, 0, _phases.Length);
        ApplyParameters(new ChorusParameters());
    }

    public override void ResetState()
    {
        for (var c = 0; c < _channels; c++)
        {
            if (_ringBuffers[c] != null) Array.Clear(_ringBuffers[c], 0, _ringBuffers[c].Length);
            _writeIndices[c] = 0;
        }
        for (var v = 0; v < VoiceCount; v++) _phases[v] = 0f;
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
            var channel = i % _channels;
            var input = buffer[idx];

            var ring = _ringBuffers[channel];
            var ringLength = _ringLengths[channel];
            var writeIndex = _writeIndices[channel];
            ring[writeIndex] = input;

            var wetSum = 0f;
            for (var v = 0; v < VoiceCount; v++)
            {
                var voicePhase = _phases[v] + v * (2f * MathF.PI / VoiceCount);
                var mod = MathF.Sin(voicePhase);
                var delaySamples = _baseOffsets[v] + depthSamples * mod;
                if (delaySamples < 1f) delaySamples = 1f;

                var intDelay = (int)delaySamples;
                var frac = delaySamples - intDelay;
                var read0 = (writeIndex - intDelay + ringLength) % ringLength;
                var read1 = (read0 - 1 + ringLength) % ringLength;
                wetSum += ring[read0] * (1f - frac) + ring[read1] * frac;
            }

            var chorusVoice = wetSum / VoiceCount;
            var processed = input * (1f - _wet) + chorusVoice * _wet;
            buffer[idx] = _dryScratch[i] * dryAmount + processed * _sendAmount;

            for (var v = 0; v < VoiceCount; v++)
            {
                _phases[v] += phaseIncrement;
                if (_phases[v] > 2f * MathF.PI) _phases[v] -= 2f * MathF.PI;
            }

            _writeIndices[channel] = (writeIndex + 1) % ringLength;
        }
    }
}
