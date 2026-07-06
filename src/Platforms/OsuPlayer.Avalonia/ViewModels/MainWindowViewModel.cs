using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Data.Models;
using OsuPlayer.Presentation.Interaction;

namespace OsuPlayer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public static MainWindowViewModel? Current { get; private set; }

    [ObservableProperty]
    public partial bool IsNavigationCollapsed { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Collection> Collection { get; set; } = [];

    public SharedVm Shared => SharedVm.Default;
    public INavigationService Navigation { get; }

    public MainWindowViewModel(INavigationService navigation)
    {
        Current = this;
        Navigation = navigation;
        IsNavigationCollapsed = AppSettings.Default?.General.IsNavigationCollapsed ?? false;
    }

    public MainWindowViewModel()
    {
        Current = this;
        Navigation = null!;
    }

    [RelayCommand]
    private void Collapse()
    {
        IsNavigationCollapsed = !IsNavigationCollapsed;
        if (AppSettings.Default != null)
        {
            AppSettings.Default.General.IsNavigationCollapsed = IsNavigationCollapsed;
            AppSettings.SaveDefault();
        }
    }
}
