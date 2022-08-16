using System.Collections.Generic;
using Milki.Extensions.Configuration;

namespace Milki.OsuPlayer.Configuration;

[Encoding("utf8")]
public class AppSettings : IConfigurationBase
{
    private GeneralSection _general = new();
    private VolumeSection _volume = new();
    private PlaySection _play = new();
    private LyricSection _lyric = new();
    private ExportSection _export = new();
    private List<HotKey> _hotKeys = new();

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

    public List<HotKey> HotKeys
    {
        get => _hotKeys ??= new List<HotKey>();
        set => _hotKeys = value;
    }

    public static AppSettings Default => ConfigurationFactory.GetConfiguration<AppSettings>(
        ".", "appsettings.yaml", MyYamlConfigurationConverter.Instance);
    public static void SaveDefault() => ConfigurationFactory.GetConfiguration<AppSettings>().Save();
}