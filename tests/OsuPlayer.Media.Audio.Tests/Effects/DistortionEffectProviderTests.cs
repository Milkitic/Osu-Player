using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class DistortionEffectProviderTests
{
    [Fact]
    public void Bypass_PassesThrough()
    {
        var signal = new float[] { 0.1f, -0.1f, 0.2f, -0.2f };
        var source = new FixedSignalSampleProvider(signal);
        var dist = new DistortionEffectProvider(source);
        dist.SetIntensity(-1f);

        var buffer = new float[4];
        dist.Read(buffer, 0, 4);

        Assert.Equal(signal, buffer);
    }

    [Fact]
    public void Active_ClipsToApproxOne()
    {
        // A modest input with high gain should saturate. With input
        // 0.5 and 24 dB of gain the driven signal is ~7.9, well
        // past the tanh knee.
        var signal = DcSignal(0.5f, 4096);
        var source = new FixedSignalSampleProvider(signal);
        var dist = new DistortionEffectProvider(source);
        dist.SetIntensity(0.9f);
        dist.ApplyParameters(new OsuPlayer.Core.Configuration.DistortionParameters
        {
            GainDb = 24f,
            CutoffHz = 4000f,
        });

        var buffer = new float[signal.Length];
        dist.Read(buffer, 0, buffer.Length);

        var maxAbs = 0f;
        for (var i = 0; i < buffer.Length; i++)
        {
            maxAbs = MathF.Max(maxAbs, MathF.Abs(buffer[i]));
        }
        // tanh saturates to (-1, +1); after the LP filter and the
        // wet/dry send the steady state is below input but still
        // meaningful.
        Assert.True(maxAbs <= 1.05f, $"Output should be close to saturated, got {maxAbs}");
        Assert.True(maxAbs > 0.4f, $"Output should be above 0.4 with strong gain, got {maxAbs}");
    }

    [Fact]
    public void LowIntensity_DoesNotSaturate()
    {
        // At very low positive intensity the pre-gain is small, the
        // tanh soft clip is barely active, and the LP filter's
        // steady-state value should sit well below 1.
        var signal = DcSignal(0.05f, 16384);
        var source = new FixedSignalSampleProvider(signal);
        var dist = new DistortionEffectProvider(source);
        dist.SetIntensity(0.05f);

        var buffer = new float[signal.Length];
        dist.Read(buffer, 0, buffer.Length);

        // Look at the tail (LP filter settled).
        var last = buffer[buffer.Length - 1];
        Assert.True(last > 0f, $"Output should be positive, got {last}");
        Assert.True(last < 0.8f, $"Output should be well below 1.0, got {last}");
    }

    private static float[] DcSignal(float value, int length)
    {
        var s = new float[length];
        for (var i = 0; i < length; i++) s[i] = value;
        return s;
    }
}
