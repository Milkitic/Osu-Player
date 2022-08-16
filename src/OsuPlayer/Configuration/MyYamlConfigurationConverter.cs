using Milki.Extensions.Configuration.Converters;
using YamlDotNet.Serialization;

namespace Milki.OsuPlayer.Configuration;

internal class MyYamlConfigurationConverter : YamlConfigurationConverter
{
    private MyYamlConfigurationConverter()
    {
    }

    public static MyYamlConfigurationConverter Instance { get; } = new();

    protected override void ConfigSerializeBuilder(SerializerBuilder builder)
    {
        base.ConfigSerializeBuilder(builder);
        builder.WithTypeConverter(new BindKeysConverter());
    }

    protected override void ConfigDeserializeBuilder(DeserializerBuilder builder)
    {
        base.ConfigDeserializeBuilder(builder);
        builder.WithTypeConverter(new BindKeysConverter());
    }
}