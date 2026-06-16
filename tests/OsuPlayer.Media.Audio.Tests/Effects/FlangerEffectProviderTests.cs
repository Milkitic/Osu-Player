using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class FlangerEffectProviderTests
{
    [Fact]
    public void Bypass_PassesThrough()
    {
        var signal = new float[] { 0.1f, -0.1f, 0.2f, -0.2f };
        var source = new FixedSignalSampleProvider(signal);
        var flanger = new FlangerEffectProvider(source);
        flanger.SetIntensity(-1f);

        var buffer = new float[4];
        flanger.Read(buffer, 0, 4);

        Assert.Equal(signal, buffer);
    }

    [Fact]
    public void Active_CombFilteringAtLowIntensity()
    {
        // A long DC signal. Flanger at low intensity (mostly dry) should
        // produce a near-unity output (the dry path passes through).
        var signal = DcSignal(0.5f, 4096);
        var source = new FixedSignalSampleProvider(signal);
        var flanger = new FlangerEffectProvider(source);
        flanger.SetIntensity(0.1f);

        var buffer = new float[signal.Length];
        flanger.Read(buffer, 0, buffer.Length);

        // Sample deep into the buffer to skip the cold-start region.
        var avg = 0f;
        var startIdx = signal.Length - 512;
        for (var i = startIdx; i < signal.Length; i++)
        {
            avg += MathF.Abs(buffer[i]);
        }
        avg /= 512;
        Assert.True(avg > 0.3f,
            $"Expected mostly-dry output around 0.5, got avg |x|={avg}");
    }

    private static float[] DcSignal(float value, int length)
    {
        var s = new float[length];
        for (var i = 0; i < length; i++) s[i] = value;
        return s;
    }
}
