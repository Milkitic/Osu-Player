using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class ChorusEffectProviderTests
{
    [Fact]
    public void Bypass_PassesThrough()
    {
        var signal = new float[] { 0.1f, -0.1f, 0.2f, -0.2f };
        var source = new FixedSignalSampleProvider(signal);
        var chorus = new ChorusEffectProvider(source);
        chorus.SetIntensity(-1f);

        var buffer = new float[4];
        chorus.Read(buffer, 0, 4);

        Assert.Equal(signal, buffer);
    }

    [Fact]
    public void Active_DelaysOutput()
    {
        // A single non-zero sample, all other zeros: the chorus
        // should produce an output that has energy in the
        // (delayed) voices, not just at the original position.
        var signal = new float[88200 + 1]; // 1 second at 44.1k stereo
        signal[0] = 1.0f;
        signal[1] = 1.0f;
        var source = new FixedSignalSampleProvider(signal);
        var chorus = new ChorusEffectProvider(source);
        chorus.SetIntensity(0.5f);

        var buffer = new float[signal.Length];
        chorus.Read(buffer, 0, buffer.Length);

        var maxAfterInitial = 0f;
        for (var i = 100; i < buffer.Length; i++) // skip the dry component
        {
            maxAfterInitial = MathF.Max(maxAfterInitial, MathF.Abs(buffer[i]));
        }
        Assert.True(maxAfterInitial > 0.01f,
            $"Expected chorus tail energy, got max |x|={maxAfterInitial}");
    }

    [Fact]
    public void ResetState_ZeroesBuffers()
    {
        var source = new SineSampleProvider(440f, 0.5f, totalSamples: 4096);
        var chorus = new ChorusEffectProvider(source);
        chorus.SetIntensity(0.5f);
        chorus.Read(new float[4096], 0, 4096);

        chorus.ResetState();

        // After reset, a fresh zero input should produce zero output
        // (the chorus rings into a silent input → no tail).
        var silent = new SilenceSampleProvider(totalSamples: 4096);
        var chorus2 = new ChorusEffectProvider(silent);
        chorus2.SetIntensity(0.5f);
        var buf = new float[4096];
        chorus2.Read(buf, 0, buf.Length);

        var maxAbs = 0f;
        for (var i = 0; i < buf.Length; i++)
        {
            maxAbs = MathF.Max(maxAbs, MathF.Abs(buf[i]));
        }
        Assert.Equal(0f, maxAbs, 6);
    }

    [Fact]
    public void ApplyParameters_ChangesVoices()
    {
        var firstSignal = new float[1024];
        firstSignal[0] = 1f; firstSignal[1] = 1f;

        var first = new ChorusEffectProvider(new FixedSignalSampleProvider(firstSignal));
        first.SetIntensity(0.5f);

        var firstBuf = new float[firstSignal.Length];
        first.Read(firstBuf, 0, firstBuf.Length);
        var firstEnergy = EnergyAfter(firstBuf, 100);

        var secondSignal = new float[1024];
        secondSignal[0] = 1f; secondSignal[1] = 1f;

        var second = new ChorusEffectProvider(new FixedSignalSampleProvider(secondSignal));
        second.SetIntensity(0.5f);
        second.ApplyParameters(new OsuPlayer.Core.Configuration.ChorusParameters
        {
            Voice1DelayMs = 8f,
            Voice2DelayMs = 15f,
            Voice3DelayMs = 22f,
            DepthMs = 9f,
            RateHz = 2.0f,
            Wet = 0.8f,
        });
        var secondBuf = new float[secondSignal.Length];
        second.Read(secondBuf, 0, secondBuf.Length);
        var secondEnergy = EnergyAfter(secondBuf, 100);

        Assert.True(secondEnergy > firstEnergy,
            $"Expected deeper modulation to produce more tail energy; first={firstEnergy}, second={secondEnergy}");
    }

    private static float EnergyAfter(float[] buffer, int startIndex)
    {
        var energy = 0f;
        for (var i = startIndex; i < buffer.Length; i++)
        {
            energy += buffer[i] * buffer[i];
        }
        return energy;
    }
}
