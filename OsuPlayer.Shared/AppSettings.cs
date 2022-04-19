using System.ComponentModel.DataAnnotations.Schema;
using OsuPlayer.Shared.Configuration;
using OsuPlayer.Shared.Configuration.Framework;

namespace OsuPlayer.Shared;

[Table("appsettings")]
public sealed class AppSettings : ConfigurationBase
{
    private SectionSoftware _software = new();
    private SectionData _data = new();
    private SectionPlay _play = new();
    private SectionHotKey _hotKey = new();

    public SectionSoftware Software
    {
        get => _software ??= new();
        set => _software = value;
    }

    public SectionData Data
    {
        get => _data ??= new();
        set => _data = value;
    }

    public SectionPlay Play
    {
        get => _play ??= new();
        set => _play = value;
    }

    public SectionHotKey HotKey
    {
        get => _hotKey ??= new();
        set => _hotKey = value;
    }
}