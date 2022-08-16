using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class LyricSection
{
    public bool EnableLyric { get; set; } = true;
    public LyricSource LyricSource { get; set; } = LyricSource.Auto;
    public LyricProvideType ProvideType { get; set; } = LyricProvideType.Original;
    public bool StrictMode { get; set; } = true;
    public bool EnableCache { get; set; } = true;
    public string FontFamily { get; set; }
    public double Hue { get; set; }
    public double Saturation { get; set; }
    public double Lightness { get; set; }
}