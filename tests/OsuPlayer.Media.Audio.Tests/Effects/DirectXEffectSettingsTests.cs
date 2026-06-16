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

    [Fact]
    public void Parameters_CarriedInSnapshot()
    {
        var settings = new DirectXEffectSettings
        {
            Kind = DirectXEffectKind.Compressor,
            Intensity = 0.5f,
            Parameters = new EffectParameterSet
            {
                Compressor = new CompressorParameters { ThresholdDb = -30f, Ratio = 10f }
            }
        };

        Assert.Equal(-30f, settings.Parameters.Compressor.ThresholdDb);
        Assert.Equal(10f, settings.Parameters.Compressor.Ratio);
    }

    [Fact]
    public void EffectsSection_ToSettings_IncludesParametersClone()
    {
        var section = new EffectsSection
        {
            Kind = DirectXEffectKind.Distortion,
            Intensity = 0.8f,
        };
        section.Parameters.Distortion.GainDb = 24f;

        var settings = section.ToSettings();

        Assert.Equal(DirectXEffectKind.Distortion, settings.Kind);
        Assert.Equal(0.8f, settings.Intensity);
        Assert.Equal(24f, settings.Parameters.Distortion.GainDb);
        Assert.NotSame(section.Parameters, settings.Parameters);
        Assert.NotSame(section.Parameters.Distortion, settings.Parameters.Distortion);
    }

    [Fact]
    public void EffectsSection_NotifyParametersChanged_RaisesPropertyChanged()
    {
        var section = new EffectsSection();
        string? raisedPropertyName = null;
        section.PropertyChanged += (_, e) => raisedPropertyName = e.PropertyName;

        section.Parameters.Compressor.ThresholdDb = -40f;
        section.NotifyParametersChanged();

        Assert.Equal(nameof(EffectsSection.Parameters), raisedPropertyName);
    }
}
