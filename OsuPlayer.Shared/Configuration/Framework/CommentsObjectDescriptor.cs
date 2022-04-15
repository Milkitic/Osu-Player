using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace OsuPlayer.Shared.Configuration.Framework;

public sealed class CommentsObjectDescriptor : IObjectDescriptor
{
    private readonly IObjectDescriptor _innerDescriptor;

    public CommentsObjectDescriptor(IObjectDescriptor innerDescriptor, string comment)
    {
        _innerDescriptor = innerDescriptor;
        Comment = comment;
    }

    public string Comment { get; private set; }

    public object? Value => _innerDescriptor.Value;
    public Type Type => _innerDescriptor.Type;
    public Type StaticType => _innerDescriptor.StaticType;
    public ScalarStyle ScalarStyle => _innerDescriptor.ScalarStyle;
}