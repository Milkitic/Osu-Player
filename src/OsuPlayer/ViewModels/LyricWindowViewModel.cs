using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.ViewModels;

public class LyricWindowViewModel : VmBase
{
    private FontFamily _fontFamily;
    private double _hue;
    private double _lightness;
    private double _saturation;
    private bool _showFrame;

    public ICollection<FontFamily> AllFontFamilies { get; } =
        new SortedSet<FontFamily>(Fonts.SystemFontFamilies.Concat(new[]
        {
            (FontFamily)Application.Current.FindResource("GenericRegular")
        }), new FontFamilyComparer(CultureInfo.CurrentUICulture));

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set
        {
            if (Equals(value, _fontFamily)) return;
            _fontFamily = value;
            AppSettings.Default.LyricSection.FontFamily = _fontFamily?.ToString();
            AppSettings.SaveDefault();
            OnPropertyChanged();
        }
    }

    public double Hue
    {
        get => _hue;
        set => this.RaiseAndSetIfChanged(ref _hue, value);
    }

    public double Lightness
    {
        get => _lightness;
        set => this.RaiseAndSetIfChanged(ref _lightness, value);
    }

    public double Saturation
    {
        get => _saturation;
        set => this.RaiseAndSetIfChanged(ref _saturation, value);
    }

    public SharedVm Shared => SharedVm.Default;

    public bool ShowFrame
    {
        get => _showFrame;
        set => this.RaiseAndSetIfChanged(ref _showFrame, value);
    }
}