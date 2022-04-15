using System.Text;
using YamlDotNet.Serialization;

namespace OsuPlayer.Shared.Configuration.Framework;

public abstract class ConfigurationBase
{
    [YamlIgnore]
    public virtual Encoding Encoding { get; } = Encoding.UTF8;

    [YamlIgnore]
    internal Func<Task>? SaveAction;

    public async Task SaveAsync()
    {
        if (SaveAction != null) await SaveAction();
    }
}