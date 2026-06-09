using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class PlayPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int GeneralOffset { get; set; }

    [ObservableProperty]
    public partial bool AutoPlay { get; set; }

    [ObservableProperty]
    public partial bool Memory { get; set; }

    [ObservableProperty]
    public partial List<string> AvailableDevices { get; set; } = new() { "Default" };

    [ObservableProperty]
    public partial string SelectedDevice { get; set; } = "Default";
}
