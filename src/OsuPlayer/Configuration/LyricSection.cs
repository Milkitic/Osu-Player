using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class LyricSection
{
    public double AppearanceHue { get; set; }
    public double AppearanceLightness { get; set; }
    public double AppearanceSaturation { get; set; }
    public string FontFamily { get; set; }
    public bool IsCacheEnabled { get; set; } = true;
    public bool IsDesktopLyricEnabled { get; set; } = true;
    public bool IsDesktopLyricLocked { get; set; } = true;
    public bool IsStrictModeEnabled { get; set; } = true;
    public LyricProvideType LyricProvideType { get; set; } = LyricProvideType.Original;
    public LyricSource LyricSource { get; set; } = LyricSource.Auto;
}