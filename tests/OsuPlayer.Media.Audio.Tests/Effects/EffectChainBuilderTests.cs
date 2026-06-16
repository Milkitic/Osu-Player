using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio.Effects;
using Xunit;

namespace OsuPlayer.Media.Audio.Tests.Effects;

public class EffectChainBuilderTests
{
    [Theory]
    [InlineData(DirectXEffectKind.Compressor)]
    [InlineData(DirectXEffectKind.Chorus)]
    [InlineData(DirectXEffectKind.Gargle)]
    [InlineData(DirectXEffectKind.ReverbEx)]
    [InlineData(DirectXEffectKind.Flanger)]
    [InlineData(DirectXEffectKind.Distortion)]
    public void Create_ReturnsProvider_ForEachKind(DirectXEffectKind kind)
    {
        var source = new FixedSignalSampleProvider(new float[256]);
        var effect = EffectChainBuilder.Create(kind, source, new EffectParameterSet(), 0.5f);

        Assert.NotNull(effect);
        Assert.Same(source.WaveFormat, effect.WaveFormat);

        var buffer = new float[256];
        var read = effect.Read(buffer, 0, buffer.Length);
        Assert.Equal(256, read);
    }

    [Fact]
    public void Create_ThrowsForNone()
    {
        var source = new FixedSignalSampleProvider(new float[16]);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EffectChainBuilder.Create(DirectXEffectKind.None, source, new EffectParameterSet(), 0.5f));
    }
}
