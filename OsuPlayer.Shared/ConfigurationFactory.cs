using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Anotar.NLog;
using OsuPlayer.Shared.Configuration.Framework;

namespace OsuPlayer.Shared;

public static class ConfigurationFactory
{
    private static readonly Dictionary<Type, ConfigurationBase> CacheDictionary = new();

    public static T GetConfiguration<T>(YamlConverter? converter = null) where T : ConfigurationBase
    {
        var t = typeof(T);

        if (CacheDictionary.TryGetValue(t, out var val))
            return (T)val;

        var filename = (t.GetCustomAttribute<TableAttribute>()?.Name ?? t.FullName) + ".yaml";
        var folder = "./configs";
        var path = Path.Combine(folder, filename);
        converter ??= new YamlConverter();
        var success = TryLoadConfigFromFile<T>(path, converter, out var config, out var ex);
        if (!success) throw ex!;
        config!.SaveAction = async () => SaveConfig(config, path, converter);
        CacheDictionary.Add(t, config);
        return config;
    }

    public static bool TryLoadConfigFromFile<T>(
        string path,
        YamlConverter converter,
        [NotNullWhen(true)] out T? config,
        [NotNullWhen(false)] out Exception? e) where T : ConfigurationBase
    {
        var success = TryLoadConfigFromFile(typeof(T), path, converter, out var config1, out e);
        config = (T?)config1;
        return success;
    }

    public static bool TryLoadConfigFromFile(
        Type type,
        string path,
        YamlConverter converter,
        [NotNullWhen(true)] out ConfigurationBase? config,
        [NotNullWhen(false)] out Exception? e)
    {
        if (!Path.IsPathRooted(path))
            path = Path.Combine(Environment.CurrentDirectory, path);

        if (!File.Exists(path))
        {
            config = CreateDefaultConfigByPath(type, path, converter);
            LogTo.Warn($"Config file \"{Path.GetFileName(path)}\" was not found. " +
                       $"Default config was created and used.");
        }
        else
        {
            var content = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(content)) content = "default:\r\n";
            try
            {
                config = converter.DeserializeSettings(content, type);
                SaveConfig(config, path, converter);
                LogTo.Debug(() => $"Config file \"{Path.GetFileName(path)}\" was loaded.");
            }
            catch (Exception ex)
            {
                config = null;
                e = ex;
                return false;
            }
        }

        e = null;
        return true;
    }

    public static ConfigurationBase CreateDefaultConfigByPath(Type type, string path, YamlConverter converter)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, "");
        var config = converter.DeserializeSettings("default:\r\n", type);
        SaveConfig(config, path, converter);
        return config;
    }

    private static void SaveConfig(ConfigurationBase config, string path, YamlConverter converter)
    {
        var content = converter.SerializeSettings(config);
        File.WriteAllText(path, content, config.Encoding);
    }
}