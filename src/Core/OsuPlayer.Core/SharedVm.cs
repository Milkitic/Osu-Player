using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Core;

public partial class SharedVm : ObservableObject
{
    [ObservableProperty]
    public partial bool EnableVideo { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = false;

    public AppSettings AppSettings { get; } = AppSettings.Default;

    public static SharedVm Default { get; } = new();

    private SharedVm()
    {
    }
}