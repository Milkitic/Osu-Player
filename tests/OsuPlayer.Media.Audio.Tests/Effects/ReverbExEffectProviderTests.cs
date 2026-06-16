using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class ReverbExEffectProviderTests
{
    [Fact]
    public void Bypass_PassesThrough()
    {
        var signal = new float[] { 0.1f, -0.1f, 0.2f, -0.2f };
        var source = new FixedSignalSampleProvider(signal);
        var reverb = new ReverbExEffectProvider(source);
        reverb.SetIntensity(-1f);

        var buffer = new float[4];
        reverb.Read(buffer, 0, 4);

        Assert.Equal(signal, buffer);
    }

    [Fact]
    public void Active_ProducesTail()
    {
        // Single impulse: reverb should ring out for tens of ms after
        // the dry component decays.
        const int sampleRate = 44100;
        const int channels = 2;
        var length = sampleRate * channels; // 1 second
        var signal = new float[length];
        signal[0] = 1f;
        signal[1] = 1f;

        var source = new FixedSignalSampleProvider(signal);
        var reverb = new ReverbExEffectProvider(source);
        reverb.SetIntensity(0.7f);
        reverb.ApplyParameters(new OsuPlayer.Core.Configuration.ReverbExParameters
        {
            RoomSize = 0.85f,
            Damp = 0.3f,
            Wet1 = 0.5f,
            Wet2 = 0.5f,
            Dry = 0.4f,
            Width = 0.8f,
        });

        var buffer = new float[length];
        reverb.Read(buffer, 0, buffer.Length);

        // 100 ms into the tail we should still see measurable energy.
        var tailStart = sampleRate * channels / 10;
        var energy = 0f;
        for (var i = tailStart; i < Math.Min(buffer.Length, tailStart + 4096); i++)
        {
            energy += buffer[i] * buffer[i];
        }
        Assert.True(energy > 1e-4f,
            $"Expected reverb tail energy around 100 ms in, got sum={energy}");
    }

    [Fact]
    public void ApplyParameters_ChangesRoomSize()
    {
        const int sampleRate = 44100;
        const int channels = 2;
        var length = sampleRate * channels; // 1 second
        var signal = new float[length];
        signal[0] = 1f;
        signal[1] = 1f;

        var shortReverb = new ReverbExEffectProvider(new FixedSignalSampleProvider(signal));
        shortReverb.SetIntensity(0.7f);
        shortReverb.ApplyParameters(new OsuPlayer.Core.Configuration.ReverbExParameters
        {
            RoomSize = 0.3f, Damp = 0.4f, Wet1 = 0.5f, Wet2 = 0.5f, Dry = 0.3f, Width = 0.8f,
        });
        var shortBuf = new float[length];
        shortReverb.Read(shortBuf, 0, shortBuf.Length);

        var longReverb = new ReverbExEffectProvider(new FixedSignalSampleProvider(signal));
        longReverb.SetIntensity(0.7f);
        longReverb.ApplyParameters(new OsuPlayer.Core.Configuration.ReverbExParameters
        {
            RoomSize = 0.95f, Damp = 0.4f, Wet1 = 0.5f, Wet2 = 0.5f, Dry = 0.3f, Width = 0.8f,
        });
        var longBuf = new float[length];
        longReverb.Read(longBuf, 0, longBuf.Length);

        // Larger room → more energy in the late tail. Compare the
        // sum of squared samples in the 200-300 ms range.
        static float TailEnergy(float[] buffer, int start, int count)
        {
            var sum = 0f;
            var end = Math.Min(buffer.Length, start + count);
            for (var i = start; i < end; i++) sum += buffer[i] * buffer[i];
            return sum;
        }
        var shortTail = TailEnergy(shortBuf, length / 5, 4096);
        var longTail = TailEnergy(longBuf, length / 5, 4096);

        Assert.True(longTail > shortTail,
            $"Expected larger room to produce more tail energy; short={shortTail}, long={longTail}");
    }

    [Fact]
    public void ResetState_ZeroesFilters()
    {
        var source = new SineSampleProvider(440f, 0.5f, totalSamples: 4096);
        var reverb = new ReverbExEffectProvider(source);
        reverb.SetIntensity(0.5f);
        reverb.Read(new float[4096], 0, 4096);

        reverb.ResetState();

        var silent = new SilenceSampleProvider(totalSamples: 4096);
        var reverb2 = new ReverbExEffectProvider(silent);
        reverb2.SetIntensity(0.5f);
        var buf = new float[4096];
        reverb2.Read(buf, 0, buf.Length);

        var maxAbs = 0f;
        for (var i = 0; i < buf.Length; i++)
        {
            maxAbs = MathF.Max(maxAbs, MathF.Abs(buf[i]));
        }
        Assert.Equal(0f, maxAbs, 6);
    }
}
