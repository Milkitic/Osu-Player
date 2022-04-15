using OsuPlayer.Shared.Configuration;
using OsuPlayer.Shared.Configuration.Framework;

namespace OsuPlayer.Shared;

public class AppSettings : ConfigurationBase
{
    private SectionSoftware _software = new();

    public SectionSoftware Software
    {
        get => _software ??= new();
        set => _software = value;
    }

    public SectionData Data { get; set; }
    public SectionPlay Play { get; set; }
    public SectionHotKey HotKey { get; set; }
}