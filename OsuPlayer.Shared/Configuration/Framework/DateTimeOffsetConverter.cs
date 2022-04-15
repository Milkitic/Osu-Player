using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace OsuPlayer.Shared.Configuration.Framework;

public class DateTimeOffsetConverter : IYamlTypeConverter
{
    private static readonly Type MemberInfo = typeof(DateTimeOffset);
    public bool Accepts(Type type)
    {
        return type == MemberInfo;
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        var s = parser.Consume<Scalar>();
        var str = s.Value;
        return DateTimeOffset.Parse(str);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        emitter.Emit(new Scalar(value?.ToString() ?? ""));
    }
}