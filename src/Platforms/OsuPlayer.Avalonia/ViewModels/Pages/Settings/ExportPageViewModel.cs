using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Avalonia.Services;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Avalonia.ViewModels.Pages.Settings;

public partial class ExportPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string MusicPath { get; set; } = "";

    [ObservableProperty]
    public partial string BgPath { get; set; } = "";

    [ObservableProperty]
    public partial ExportNamingStyle NamingStyle { get; set; } = ExportNamingStyle.Title;

    [ObservableProperty]
    public partial ExportGroupStyle GroupStyle { get; set; } = ExportGroupStyle.None;

    [RelayCommand]
    private void BrowseMusic()
    {
        AppNotificationService.Instance.Push("Folder picker not yet implemented in Avalonia build.");
    }

    [RelayCommand]
    private void BrowseBg()
    {
        AppNotificationService.Instance.Push("Folder picker not yet implemented in Avalonia build.");
    }
}
