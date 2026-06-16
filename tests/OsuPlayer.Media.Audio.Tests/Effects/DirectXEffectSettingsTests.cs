using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class DirectXEffectSettingsTests
{
    [Fact]
    public void Disabled_IsNotActive()
    {
        Assert.False(DirectXEffectSettings.Disabled.IsEffectActive);
    }

    [Fact]
    public void IsEffectActive_RespectsIntensity()
    {
        var noneWithIntensity = new DirectXEffectSettings { Kind = DirectXEffectKind.Chorus, Intensity = 0.5f };
        Assert.True(noneWithIntensity.IsEffectActive);

        var kindButBypass = new DirectXEffectSettings { Kind = DirectXEffectKind.ReverbEx, Intensity = -1f };
        Assert.False(kindButBypass.IsEffectActive);

        var noKind = new DirectXEffectSettings { Kind = DirectXEffectKind.None, Intensity = 0.5f };
        Assert.False(noKind.IsEffectActive);
    }

    [Fact]
    public void Equals_ComparesAllFields()
    {
        var a = new DirectXEffectSettings { Kind = DirectXEffectKind.Flanger, Intensity = 0.3f, ApplyToMusic = true };
        var b = new DirectXEffectSettings { Kind = DirectXEffectKind.Flanger, Intensity = 0.3f, ApplyToMusic = true };
        var c = new DirectXEffectSettings { Kind = DirectXEffectKind.Flanger, Intensity = 0.4f, ApplyToMusic = true };

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
