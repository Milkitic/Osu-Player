using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class EffectChainSampleProviderTests
{
    private static EffectChainSampleProvider NewChain(FixedSignalSampleProvider source)
        => new(source, new EffectParameterSet());

    [Fact]
    public void DefaultChain_PassesThroughUntouched()
    {
        var source = new FixedSignalSampleProvider(new float[] { 0.1f, -0.2f, 0.3f, -0.4f });
        var chain = NewChain(source);

        var buffer = new float[4];
        var read = chain.Read(buffer, 0, 4);

        Assert.Equal(4, read);
        Assert.Equal(new float[] { 0.1f, -0.2f, 0.3f, -0.4f }, buffer);
        Assert.Equal(DirectXEffectKind.None, chain.ActiveKind);
    }

    [Fact]
    public void SetEffect_None_DisablesActiveEffect()
    {
        var source = new FixedSignalSampleProvider(new float[] { 0.5f, 0.5f, 0.5f, 0.5f });
        var chain = NewChain(source);
        chain.SetEffect(DirectXEffectKind.Distortion, 0.5f);
        Assert.Equal(DirectXEffectKind.Distortion, chain.ActiveKind);

        chain.SetEffect(DirectXEffectKind.None, 0f);
        Assert.Equal(DirectXEffectKind.None, chain.ActiveKind);

        var buffer = new float[4];
        chain.Read(buffer, 0, 4);
        Assert.Equal(new float[] { 0.5f, 0.5f, 0.5f, 0.5f }, buffer);
    }

    [Fact]
    public void SetEffect_BelowBypassThreshold_Disables()
    {
        var source = new FixedSignalSampleProvider(new float[] { 0.5f, 0.5f });
        var chain = NewChain(source);
        chain.SetEffect(DirectXEffectKind.ReverbEx, -1f);
        Assert.Equal(DirectXEffectKind.None, chain.ActiveKind);
    }

    [Fact]
    public void SetEffect_SameEffect_UpdatesIntensityInPlace()
    {
        var source = new FixedSignalSampleProvider(new float[] { 0.1f });
        var chain = NewChain(source);
        chain.SetEffect(DirectXEffectKind.Distortion, 0.2f);
        var firstKind = chain.ActiveKind;
        var firstIntensity = chain.ActiveIntensity;

        chain.SetEffect(DirectXEffectKind.Distortion, 0.7f);
        Assert.Equal(firstKind, chain.ActiveKind);
        Assert.NotEqual(firstIntensity, chain.ActiveIntensity);
    }

    [Fact]
    public void SetEffect_SwitchesEffectProvider()
    {
        var source = new FixedSignalSampleProvider(new float[] { 0.1f, -0.1f });
        var chain = NewChain(source);
        chain.SetEffect(DirectXEffectKind.Distortion, 0.5f);
        Assert.Equal(DirectXEffectKind.Distortion, chain.ActiveKind);

        chain.SetEffect(DirectXEffectKind.Flanger, 0.3f);
        Assert.Equal(DirectXEffectKind.Flanger, chain.ActiveKind);
    }

    [Fact]
    public void WaveFormat_DelegatesToSource()
    {
        var source = new FixedSignalSampleProvider(new float[0]);
        var chain = NewChain(source);
        Assert.Same(source.WaveFormat, chain.WaveFormat);
    }

    [Fact]
    public void ApplyActiveParameters_NoOp_WhenNoEffect()
    {
        var source = new FixedSignalSampleProvider(new float[] { 0.1f });
        var chain = NewChain(source);
        // Should not throw when no effect is active.
        chain.ApplyActiveParameters();
    }
}
