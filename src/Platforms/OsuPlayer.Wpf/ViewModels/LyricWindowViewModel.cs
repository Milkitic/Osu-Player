using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Playback;

namespace OsuPlayer.ViewModels;

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
    public partial string FontFamily { get; set; }

    partial void OnFontFamilyChanged(string value)
    {
        AppSettings.Default.Lyric.FontFamily = value;
        AppSettings.SaveDefault();
    }

    [ObservableProperty]
    public partial double Hue { get; set; }

    [ObservableProperty]
    public partial double Saturation { get; set; }

    [ObservableProperty]
    public partial double Lightness { get; set; }
}
