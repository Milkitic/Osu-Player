using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.ViewModels;

public partial class LyricWindowViewModel : ObservableObject
{
    public ObservablePlayController Controller { get; }
    public SharedVm Shared { get; }

    public LyricWindowViewModel(ObservablePlayController controller, SharedVm shared)
    {
        Controller = controller;
        Shared = shared;
    }

    [ObservableProperty]
    public partial bool ShowFrame { get; set; }

    [ObservableProperty]
    public partial bool IsLyricWindowShown { get; set; }

    [ObservableProperty]
    public partial object FontFamily { get; set; }

    partial void OnFontFamilyChanged(object value)
    {
        AppSettings.Default.Lyric.FontFamily = value?.ToString();
        AppSettings.SaveDefault();
    }

    public ICollection<FontFamily> AllFontFamilies { get; } =
        new SortedSet<FontFamily>(Fonts.SystemFontFamilies.Concat(new[]
        {
            (FontFamily)Application.Current.FindResource("SspRegular")
        }), new FontFamilyComparer());

    [ObservableProperty]
    public partial double Hue { get; set; }

    [ObservableProperty]
    public partial double Saturation { get; set; }

    [ObservableProperty]
    public partial double Lightness { get; set; }
}