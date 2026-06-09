using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Avalonia.Services;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Avalonia.ViewModels.Pages.Settings;

public partial class LyricPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool EnableLyric { get; set; } = true;

    [ObservableProperty]
    public partial LyricSource LyricSource { get; set; } = LyricSource.Auto;

    [ObservableProperty]
    public partial LyricProvideType ProvideType { get; set; } = LyricProvideType.Original;

    [ObservableProperty]
    public partial bool StrictMode { get; set; } = true;

    [ObservableProperty]
    public partial bool EnableCache { get; set; } = true;
}
