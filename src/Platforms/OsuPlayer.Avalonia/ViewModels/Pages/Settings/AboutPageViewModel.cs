using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Services;
using OsuPlayer.Shared;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class AboutPageViewModel : ObservableObject
{
    private const string DtFormat = "g";
    private readonly UpdateInst _updateInst;

    public AboutPageViewModel(UpdateInst updateInst)
    {
        _updateInst = updateInst;
        CurrentVersion = _updateInst.CurrentVersion;
        HasUpdate = _updateInst.NewRelease != null;
        UpdateLastUpdateText();
    }

    [ObservableProperty]
    public partial string CurrentVersion { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastUpdateCheckText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasUpdate { get; set; }

    [ObservableProperty]
    public partial bool IsCheckingUpdate { get; set; }

    private void UpdateLastUpdateText()
    {
        LastUpdateCheckText = AppSettings.Default?.LastUpdateCheck == null
            ? I18NUtil.GetString("ui-sets-content-never")
            : AppSettings.Default.LastUpdateCheck.Value.ToString(DtFormat);
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        IsCheckingUpdate = true;
        try
        {
            var hasNew = await _updateInst.CheckUpdateAsync();
            if (AppSettings.Default != null)
            {
                AppSettings.Default.LastUpdateCheck = DateTime.Now;
                UpdateLastUpdateText();
                AppSettings.SaveDefault();
            }

            if (hasNew == true)
            {
                HasUpdate = true;
                ShowNewVersionDialog();
            }
            else
            {
                AppNotificationService.Instance.Push(I18NUtil.GetString("ui-sets-content-alreadyNewest"));
            }
        }
        catch (Exception ex)
        {
            AppNotificationService.Instance.Push(I18NUtil.GetString("ui-sets-content-errorWhileCheckingUpdate") +
                                                Environment.NewLine +
                                                (ex.InnerException?.Message ?? ex.Message));
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private void ShowNewVersionDialog()
    {
        if (_updateInst.NewRelease != null)
        {
            AppNotificationService.Instance.Push($"{I18NUtil.GetString("ui-sets-content-hasNewVersion")}: {_updateInst.NewRelease.NewVerString}");
        }
    }

    [RelayCommand]
    private void OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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
