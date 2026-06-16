using System;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Media.Audio.Effects;

/// <summary>
/// "Reverb Ex" — modelled on the DirectSound <c>IDirectSoundFXWavesReverb</c>
/// parameter set (in-game faders for room, damping, stereo width,
/// wet/dry). Implemented as a Freeverb-style network of lowpass-
/// feedback comb filters followed by allpass filters.
/// </summary>
internal sealed class ReverbExEffectProvider : DirectXEffectProviderBase
{
    private const int CombCount = 8;
    private const int AllpassCount = 4;

    // Freeverb tuning, 44.1 kHz reference. At other sample rates the
    // delay lengths are scaled proportionally.
    private static readonly int[] CombTuning =
    {
        1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617
    };

    private static readonly int[] AllpassTuning =
    {
        556, 441, 341, 225
    };

    private readonly LBCF[] _combs;
    private readonly APF[] _allpasses;
    private float _lastWet;

    private float _roomSize;
    private float _damp;
    private float _wet1;
    private float _wet2;
    private float _dry;
    private float _width;
    private float _sendAmount;
    private bool _bypass = true;
    private float[] _dryScratch = Array.Empty<float>();

    public ReverbExEffectProvider(ISampleProvider source) : base(source)
    {
        var scale = source.WaveFormat.SampleRate / 44100f;
        _combs = new LBCF[CombCount];
        for (var c = 0; c < CombCount; c++)
        {
            _combs[c] = new LBCF(Math.Max(1, (int)(CombTuning[c] * scale)));
        }
        _allpasses = new APF[AllpassCount];
        for (var a = 0; a < AllpassCount; a++)
        {
            _allpasses[a] = new APF(Math.Max(1, (int)(AllpassTuning[a] * scale)));
        }
    }

    public override void SetIntensity(float intensity)
    {
        _sendAmount = WetFromIntensity(intensity);
        _bypass = intensity <= BypassThreshold;
    }

    public void ApplyParameters(ReverbExParameters p)
    {
        ArgumentNullException.ThrowIfNull(p);
        _roomSize = p.RoomSize;
        _damp = p.Damp;
        _wet1 = p.Wet1;
        _wet2 = p.Wet2;
        _dry = p.Dry;
        _width = p.Width;
        _bypass = false;
    }

    public void ResetToDefaults()
    {
        _sendAmount = 0.5f;
        _bypass = false;
        _lastWet = 0f;
        ApplyParameters(new ReverbExParameters());
    }

    public override void ResetState()
    {
        for (var c = 0; c < CombCount; c++) _combs[c].Reset();
        for (var a = 0; a < AllpassCount; a++) _allpasses[a].Reset();
        _lastWet = 0f;
    }

    protected override void Process(float[] buffer, int offset, int count)
    {
        if (_bypass) return;

        var channels = WaveFormat.Channels;
        var dryAmount = 1f - _sendAmount;

        if (_dryScratch.Length < count) _dryScratch = new float[count];
        Array.Copy(buffer, offset, _dryScratch, 0, count);

        for (var i = 0; i < count; i += channels)
        {
            float mono;
            if (channels == 1)
            {
                mono = buffer[offset + i];
            }
            else
            {
                mono = 0.5f * (buffer[offset + i] + buffer[offset + i + 1]);
            }

            var combSum = 0f;
            for (var c = 0; c < CombCount; c++)
            {
                combSum += _combs[c].Process(mono, _damp, _roomSize);
            }
            combSum /= CombCount;

            var allpassOut = combSum;
            for (var a = 0; a < AllpassCount; a++)
            {
                allpassOut = _allpasses[a].Process(allpassOut);
            }

            var wetL = allpassOut * (_width * 0.5f + 0.5f) + _lastWet * (_width * 0.5f - 0.5f);
            var wetR = allpassOut * (_width * 0.5f - 0.5f) + _lastWet * (_width * 0.5f + 0.5f);
            _lastWet = allpassOut;

            float leftOutput;
            float rightOutput;
            if (channels == 1)
            {
                leftOutput = mono * _dry + allpassOut * (_wet1 + _wet2) * 0.5f;
                rightOutput = leftOutput;
            }
            else
            {
                leftOutput = _dryScratch[i] * _dry + wetL * _wet1;
                rightOutput = _dryScratch[i + 1] * _dry + wetR * _wet2;
            }

            // Apply the master wet/dry send.
            if (channels == 1)
            {
                buffer[offset + i] = _dryScratch[i] * dryAmount + leftOutput * _sendAmount;
            }
            else
            {
                buffer[offset + i] = _dryScratch[i] * dryAmount + leftOutput * _sendAmount;
                buffer[offset + i + 1] = _dryScratch[i + 1] * dryAmount + rightOutput * _sendAmount;
            }
        }
    }

    private sealed class LBCF
    {
        private readonly float[] _buffer;
        private int _index;
        private float _lastOut;

        public LBCF(int length)
        {
            _buffer = new float[length];
        }

        public float Process(float input, float damp, float roomSize)
        {
            var output = _buffer[_index];
            _lastOut = output * (1f - damp) + _lastOut * damp;
            _buffer[_index] = input + _lastOut * roomSize;
            _index = (_index + 1) % _buffer.Length;
            return output;
        }

        public void Reset()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _index = 0;
            _lastOut = 0f;
        }
    }

    private sealed class APF
    {
        private readonly float[] _buffer;
        private int _index;
        private const float Feedback = 0.5f;

        public APF(int length)
        {
            _buffer = new float[length];
        }

        public float Process(float input)
        {
            var bufout = _buffer[_index];
            var output = -input + bufout;
            _buffer[_index] = input + bufout * Feedback;
            _index = (_index + 1) % _buffer.Length;
            return output;
        }

        public void Reset()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _index = 0;
        }
    }
}
