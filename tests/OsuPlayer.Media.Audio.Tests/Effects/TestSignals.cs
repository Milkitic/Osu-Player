using NAudio.Wave;

namespace OsuPlayer.Media.Audio.Tests.Effects;

/// <summary>
/// A minimal <see cref="ISampleProvider"/> used by the effect tests
/// to inject a deterministic waveform. It does no DSP of its own; it
/// simply hands the test buffer back unchanged.
/// </summary>
internal sealed class FixedSignalSampleProvider : ISampleProvider
{
    public WaveFormat WaveFormat { get; }
    private readonly float[] _samples;
    private int _position;

    public FixedSignalSampleProvider(float[] samples, int sampleRate = 44100, int channels = 2)
    {
        _samples = samples;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_position >= _samples.Length) return 0;
        var available = Math.Min(count, _samples.Length - _position);
        Array.Copy(_samples, _position, buffer, offset, available);
        _position += available;
        return available;
    }
}

/// <summary>
/// Sine wave generator for tests that want a longer, smooth signal
/// (e.g. compressor envelope follower, reverb tail).
/// </summary>
internal sealed class SineSampleProvider : ISampleProvider
{
    public WaveFormat WaveFormat { get; }
    public float Frequency { get; }
    public float Amplitude { get; }
    private readonly int _totalSamples;
    private int _position;

    public SineSampleProvider(float frequencyHz, float amplitude, int sampleRate = 44100, int channels = 2, int totalSamples = -1)
    {
        Frequency = frequencyHz;
        Amplitude = amplitude;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _totalSamples = totalSamples < 0 ? sampleRate * channels * 2 : totalSamples;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var produced = 0;
        var sampleRate = WaveFormat.SampleRate;
        while (produced < count && _position < _totalSamples)
        {
            var t = (float)((_position / WaveFormat.Channels) % sampleRate) / sampleRate;
            var s = Amplitude * MathF.Sin(2f * MathF.PI * Frequency * t);
            buffer[offset + produced] = s;
            produced++;
            _position++;
        }
        return produced;
    }
}

/// <summary>
/// Silently emits zeros; useful for proving the effect doesn't crash on
/// silence (e.g. when a hitsound bus is inactive).
/// </summary>
internal sealed class SilenceSampleProvider : ISampleProvider
{
    public WaveFormat WaveFormat { get; }
    private readonly int _totalSamples;
    private int _position;

    public SilenceSampleProvider(int sampleRate = 44100, int channels = 2, int totalSamples = 4096)
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        _totalSamples = totalSamples;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_position >= _totalSamples) return 0;
        var available = Math.Min(count, _totalSamples - _position);
        Array.Clear(buffer, offset, available);
        _position += available;
        return available;
    }
}
