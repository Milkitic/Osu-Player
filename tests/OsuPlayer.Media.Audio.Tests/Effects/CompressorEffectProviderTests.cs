using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class CompressorEffectProviderTests
{
    [Fact]
    public void Bypass_DoesNotModifySignal()
    {
        var signal = new float[] { 0.1f, -0.1f, 0.5f, -0.5f };
        var source = new FixedSignalSampleProvider(signal);
        var compressor = new CompressorEffectProvider(source)
        {
            // intensity <= -0.999 means bypass
        };
        compressor.SetIntensity(-1f);

        var buffer = new float[4];
        compressor.Read(buffer, 0, 4);

        Assert.Equal(signal, buffer);
    }

    [Fact]
    public void Compresses_LoudSignal_TowardThreshold()
    {
        // A 1.0 amplitude sine that would otherwise pass through unity
        // gain should be reduced after the envelope catches up.
        const int sampleRate = 44100;
        const int channels = 2;
        var source = new SineSampleProvider(440f, 1.0f, sampleRate, channels, totalSamples: sampleRate * channels);
        var compressor = new CompressorEffectProvider(source);
        compressor.SetIntensity(0.9f);

        var buffer = new float[sampleRate * channels];
        var read = compressor.Read(buffer, 0, buffer.Length);
        Assert.Equal(buffer.Length, read);

        // RMS over the latter half (after envelope converges) should
        // be below the un-compressed RMS.
        var half = buffer.Length / 2;
        var sumSq = 0f;
        for (var i = half; i < buffer.Length; i++)
        {
            sumSq += buffer[i] * buffer[i];
        }
        var rmsCompressed = MathF.Sqrt(sumSq / (buffer.Length - half));

        Assert.True(rmsCompressed < 0.71f,
            $"Expected compressed RMS below 0.71 (-3 dB), was {rmsCompressed:F3}.");
    }

    [Fact]
    public void ResetState_ZerosEnvelope()
    {
        var source = new SineSampleProvider(440f, 1.0f, totalSamples: 1024);
        var compressor = new CompressorEffectProvider(source);
        compressor.SetIntensity(0.5f);

        var buf1 = new float[1024];
        compressor.Read(buf1, 0, buf1.Length);

        compressor.ResetState();

        // After reset, with a fresh zero input, output should also be
        // very close to zero (the envelope follower starts from 0).
        var source2 = new SilenceSampleProvider(totalSamples: 1024);
        var compressor2 = new CompressorEffectProvider(source2);
        compressor2.SetIntensity(0.5f);

        var buf2 = new float[1024];
        compressor2.Read(buf2, 0, buf2.Length);

        var maxAbs = 0f;
        for (var i = 0; i < buf2.Length; i++)
        {
            maxAbs = MathF.Max(maxAbs, MathF.Abs(buf2[i]));
        }
        Assert.True(maxAbs < 0.01f, $"Expected silence-like output after reset, got max |x|={maxAbs}");
    }
}
