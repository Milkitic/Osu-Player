using System.IO;
using Milki.Extensions.Configuration;

namespace Milki.OsuPlayer.Configuration;

[Encoding("utf-8")]
public sealed class AppSettings : IConfigurationBase
{
    private GeneralSection _general = new();
    private VolumeSection _volume = new();
    private PlaySection _play = new();
    private LyricSection _lyric = new();
    private ExportSection _export = new();
    private BindKeySection _hotKeys = new();

    public GeneralSection GeneralSection
    {
        get => _general ??= new GeneralSection();
        set => _general = value;
    }

    public VolumeSection VolumeSection
    {
        get => _volume ??= new VolumeSection();
        set => _volume = value;
    }

    public PlaySection PlaySection
    {
        get => _play ??= new PlaySection();
        set => _play = value;
    }

    public LyricSection LyricSection
    {
        get => _lyric ??= new LyricSection();
        set => _lyric = value;
    }

    public ExportSection ExportSection
    {
        get => _export ??= new ExportSection();
        set => _export = value;
    }

    public BindKeySection HotKeys
    {
        get => _hotKeys ??= new BindKeySection();
        set => _hotKeys = value;
    }

    public static AppSettings Default => ConfigurationFactory.GetConfiguration<AppSettings>(
        ".", "AppSettings.yaml", MyYamlConfigurationConverter.Instance);
    public static void SaveDefault() => ConfigurationFactory.GetConfiguration<AppSettings>().Save();

    public static class Directories
    {
        static Directories()
        {
            Type t = typeof(Directories);
            var infos = t.GetProperties();
            foreach (var item in infos)
            {
                if (!item.Name.EndsWith("Dir")) continue;
                try
                {
                    string path = (string)item.GetValue(null, null);
                    if (path == null) continue;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        public static string CacheDir => Path.Combine(Environment.CurrentDirectory, "caches");
        public static string LyricCacheDir => Path.Combine(CacheDir, "lyrics");
        public static string ThumbCacheDir => Path.Combine(CacheDir, "thumbs");

        public static string LanguageDir => Path.Combine(Environment.CurrentDirectory, "languages");
        public static string ResourceDir => Path.Combine(Environment.CurrentDirectory, "resources");
        public static string OfficialBgDir => Path.Combine(Environment.CurrentDirectory, "official");
        public static string ToolDir => Path.Combine(Environment.CurrentDirectory, "tools");
    }
}