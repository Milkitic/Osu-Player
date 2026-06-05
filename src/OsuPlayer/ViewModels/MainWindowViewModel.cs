using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Data.Models;
using OsuPlayer.Presentation.Interaction;

namespace OsuPlayer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public static MainWindowViewModel Current { get; private set; }

    public MainWindowViewModel()
    {
        Current = this;
    }

    [ObservableProperty]
    public partial LyricWindowViewModel LyricWindowViewModel { get; set; }

    [ObservableProperty]
    public partial bool IsNavigationCollapsed { get; set; }

    [ObservableProperty]
    public partial bool IsLyricWindowLocked { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Collection> Collection { get; set; }

    [RelayCommand]
    private void Collapse()
    {
        Execute.OnUiThread(() =>
        {
            IsNavigationCollapsed = !IsNavigationCollapsed;
            AppSettings.Default.General.IsNavigationCollapsed = IsNavigationCollapsed;
            AppSettings.SaveDefault();
        });
    }
}