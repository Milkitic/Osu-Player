using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class GargleEffectProviderTests
{
    [Fact]
    public void Bypass_PassesThrough()
    {
        var signal = new float[] { 0.1f, -0.1f, 0.2f, -0.2f, 0.3f, -0.3f };
        var source = new FixedSignalSampleProvider(signal);
        var gargle = new GargleEffectProvider(source);
        gargle.SetIntensity(-1f);

        var buffer = new float[6];
        gargle.Read(buffer, 0, 6);

        Assert.Equal(signal, buffer);
    }

    [Fact]
    public void Active_ModulatesAmplitude()
    {
        // Constant DC input; if gargle is working, the output should
        // vary over time as the LFO sweeps.
        var dcSource = new FixedSignalSampleProvider(DcSignal(0.5f, 88200));
        var gargle = new GargleEffectProvider(dcSource);
        gargle.SetIntensity(0.8f);
        gargle.ApplyParameters(new OsuPlayer.Core.Configuration.GargleParameters
        {
            RateHz = 6f,
            Depth = 0.9f,
            Waveform = OsuPlayer.Core.Configuration.GargleWaveform.Triangle,
        });

        var buffer = new float[88200];
        gargle.Read(buffer, 0, buffer.Length);

        var min = float.MaxValue;
        var max = float.MinValue;
        for (var i = 0; i < buffer.Length; i++)
        {
            min = MathF.Min(min, buffer[i]);
            max = MathF.Max(max, buffer[i]);
        }

        Assert.True(min < 0.4f, $"Expected modulation to dip below 0.4, got min={min}");
        Assert.True(max > 0.45f, $"Expected modulation to peak above 0.45, got max={max}");
    }

    [Fact]
    public void ApplyParameters_ChangesRate()
    {
        // Constant DC input at 0.5; square wave at depth=0.8 should
        // produce output that toggles between 0.5 (loud) and 0.1
        // (quiet) before the wet/dry mix.
        var source = new FixedSignalSampleProvider(DcSignal(0.5f, 88200));
        var gargle = new GargleEffectProvider(source);
        gargle.SetIntensity(0.5f);

        gargle.ApplyParameters(new OsuPlayer.Core.Configuration.GargleParameters
        {
            RateHz = 1f,
            Depth = 0.8f,
            Waveform = OsuPlayer.Core.Configuration.GargleWaveform.Square,
        });

        var buffer = new float[88200];
        gargle.Read(buffer, 0, buffer.Length);

        // Wet/dry mix at 0.5 send: loud = 0.5*0.5 + 0.5*0.5 = 0.5
        //                           quiet = 0.5*0.5 + 0.1*0.5 = 0.3
        var hasLoudRegion = false;
        var hasQuietRegion = false;
        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] > 0.45f) hasLoudRegion = true;
            if (buffer[i] < 0.35f) hasQuietRegion = true;
        }
        Assert.True(hasLoudRegion, $"Expected a loud region; samples above 0.45");
        Assert.True(hasQuietRegion, $"Expected a quiet region; samples below 0.35");
    }

    private static float[] DcSignal(float value, int length)
    {
        var s = new float[length];
        for (var i = 0; i < length; i++) s[i] = value;
        return s;
    }
}
