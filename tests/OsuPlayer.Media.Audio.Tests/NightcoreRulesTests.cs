using OsuPlayer.Media.Audio.Rules;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests;

public class NightcoreRulesTests
{
    [Theory]
    [InlineData(1.5f, false, true)]
    [InlineData(1.5f, true, false)]
    [InlineData(1.0f, false, false)]
    [InlineData(1.0f, true, false)]
    [InlineData(0.75f, false, false)]
    [InlineData(0.75f, true, false)]
    [InlineData(2.0f, false, false)]
    public void ShouldEnableNightcoreBeats_MatchesExpected(float rate, bool keepTune, bool expected)
    {
        Assert.Equal(expected, NightcoreRules.ShouldEnableNightcoreBeats(rate, keepTune));
    }

    [Fact]
    public void ShouldEnableNightcoreBeats_AllowsFloatingPointDrift()
    {
        // Persisted settings may round-trip through JSON and lose a fraction
        // of a frame. The epsilon absorbs that without changing behaviour.
        Assert.True(NightcoreRules.ShouldEnableNightcoreBeats(1.5005f, false));
        Assert.True(NightcoreRules.ShouldEnableNightcoreBeats(1.4995f, false));
    }

    [Fact]
    public void NightcoreRateConstant_MatchesOsuConvention()
    {
        // The DoubleTime / NightCore mods in osu! both set rate to 1.5x.
        // If this ever changes the audio session must be updated too, so
        // pin the constant to make the dependency explicit.
        Assert.Equal(1.5f, NightcoreRules.NightcoreRate);
    }
}
