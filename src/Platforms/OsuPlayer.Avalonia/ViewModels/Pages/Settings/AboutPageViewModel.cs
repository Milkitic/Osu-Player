using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Avalonia.Interaction;
using OsuPlayer.Avalonia.Services;

namespace OsuPlayer.Avalonia.ViewModels.Pages.Settings;

public partial class AboutPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string CurrentVersion { get; set; } = "1.0.0";

    [ObservableProperty]
    public partial string LastUpdateCheckText { get; set; } = "Never";

    [ObservableProperty]
    public partial bool HasUpdate { get; set; }

    [ObservableProperty]
    public partial bool IsCheckingUpdate { get; set; }

    [RelayCommand]
    private void CheckUpdate()
    {
        // Avalonia 端 stub:实际检查更新逻辑后续接入
        IsCheckingUpdate = true;
        AppNotificationService.Instance.Push("Update check is not yet implemented in Avalonia build.");
        IsCheckingUpdate = false;
    }

    [RelayCommand]
    private void ShowNewVersionDialog()
    {
        // TODO: 集成 NewVersionWindow
    }

    [RelayCommand]
    private void OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    [RelayCommand]
    private void ShowPrivacyPolicy()
    {
        AppNotificationService.Instance.Push("This software will NOT collect any user information.");
    }
}
