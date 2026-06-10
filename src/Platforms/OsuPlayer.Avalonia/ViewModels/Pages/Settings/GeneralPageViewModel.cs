using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Services;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class GeneralPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool RunOnStartup { get; set; }

    [ObservableProperty]
    public partial string DbPath { get; set; } = "";

    [ObservableProperty]
    public partial string CustomSongsPath { get; set; } = "";

    public bool IsMinimizeWhenClosed
    {
        get => !IsExitWhenClosed;
        set { if (value) IsExitWhenClosed = false; }
    }

    public bool IsExitWhenClosed
    {
        get => _isExitWhenClosed;
        set
        {
            if (_isExitWhenClosed == value) return;
            _isExitWhenClosed = value;
            OnPropertyChanged(nameof(IsExitWhenClosed));
            OnPropertyChanged(nameof(IsMinimizeWhenClosed));
        }
    }
    private bool _isExitWhenClosed;

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
