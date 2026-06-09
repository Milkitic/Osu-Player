using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core;

namespace OsuPlayer.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsNavigationCollapsed { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    public SharedVm Shared => SharedVm.Default;
}
