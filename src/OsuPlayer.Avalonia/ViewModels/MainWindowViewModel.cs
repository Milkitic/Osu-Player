using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Avalonia.Interaction;
using OsuPlayer.Core;
using OsuPlayer.Shared;

namespace OsuPlayer.Avalonia.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsNavigationCollapsed { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial string CurrentSongTitle { get; set; } = "-";

    public SharedVm Shared => SharedVm.Default;

    public INavigationService Navigation { get; }

    public MainWindowViewModel(INavigationService navigation)
    {
        Navigation = navigation;
    }

    public MainWindowViewModel()
    {
        if (!Design.IsDesignMode) throw new InvalidOperationException();
    }
}
