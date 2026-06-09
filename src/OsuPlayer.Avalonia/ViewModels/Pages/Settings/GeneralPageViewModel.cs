using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
// using retained
using OsuPlayer.Avalonia.Services;
using OsuPlayer.Core;

namespace OsuPlayer.Avalonia.ViewModels.Pages.Settings;

public partial class GeneralPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool RunOnStartup { get; set; }

    [ObservableProperty]
    public partial string DbPath { get; set; } = "";

    [ObservableProperty]
    public partial string CustomSongsPath { get; set; } = "";

    [ObservableProperty]
    public partial bool IsMinimizeWhenClosed { get; set; } = true;

    [ObservableProperty]
    public partial bool IsExitWhenClosed { get; set; }

    [ObservableProperty]
    public partial bool SetAsDefaultOptions { get; set; }

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [RelayCommand]
    private void BrowseDb()
    {
        AppNotificationService.Instance.Push("Folder picker not yet implemented in Avalonia build.");
    }

    [RelayCommand]
    private void BrowseCustom()
    {
        AppNotificationService.Instance.Push("Folder picker not yet implemented in Avalonia build.");
    }

    [RelayCommand]
    private void SyncNow()
    {
        AppNotificationService.Instance.Push("Sync started (stub).");
    }

    [RelayCommand]
    private void ScanNow()
    {
        IsScanning = true;
        AppNotificationService.Instance.Push("Scan started (stub).");
        IsScanning = false;
    }

    [RelayCommand]
    private void CancelScan()
    {
        IsScanning = false;
    }
}
