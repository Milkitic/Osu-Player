using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Services;
using OsuPlayer.Utils;
using OsuPlayer.Lang;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class GeneralPageViewModel : ObservableObject
{
    private readonly OsuFileScanner _osuFileScanner;
    private readonly OsuDbInst _osuDbInst;

    public FileScannerViewModel ScannerViewModel => _osuFileScanner.ViewModel;

    public bool RunOnStartup
    {
        get => AppSettings.Default?.General.RunOnStartup == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.General.RunOnStartup == value) return;
            AppSettings.Default.General.RunOnStartup = value;
            OnPropertyChanged();

#if WINDOWS
            try
            {
                using var rKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                if (value)
                {
                    rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                }
                else
                {
                    rKey?.DeleteValue("OsuPlayer", false);
                }
            }
            catch
            {
            }
#endif

            AppSettings.SaveDefault();
        }
    }

    public string DbPath
    {
        get => AppSettings.Default?.General.DbPath ?? string.Empty;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.General.DbPath == value) return;
            AppSettings.Default.General.DbPath = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public string CustomSongsPath
    {
        get => AppSettings.Default?.General.CustomSongsPath ?? string.Empty;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.General.CustomSongsPath == value) return;
            AppSettings.Default.General.CustomSongsPath = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public bool IsMinimizeWhenClosed
    {
        get => AppSettings.Default?.General.ExitWhenClosed == false;
        set
        {
            if (AppSettings.Default == null || !value || AppSettings.Default.General.ExitWhenClosed == false) return;
            AppSettings.Default.General.ExitWhenClosed = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsExitWhenClosed));
            OnPropertyChanged(nameof(SetAsDefaultOptions));
            AppSettings.SaveDefault();
        }
    }

    public bool IsExitWhenClosed
    {
        get => AppSettings.Default?.General.ExitWhenClosed == true;
        set
        {
            if (AppSettings.Default == null || !value || AppSettings.Default.General.ExitWhenClosed == true) return;
            AppSettings.Default.General.ExitWhenClosed = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsMinimizeWhenClosed));
            OnPropertyChanged(nameof(SetAsDefaultOptions));
            AppSettings.SaveDefault();
        }
    }

    public bool SetAsDefaultOptions
    {
        get => AppSettings.Default?.General.ExitWhenClosed.HasValue == true;
        set
        {
            if (AppSettings.Default == null || SetAsDefaultOptions == value) return;
            AppSettings.Default.General.ExitWhenClosed = value ? IsExitWhenClosed : null;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public bool IsScanning => ScannerViewModel.IsScanning;

    public GeneralPageViewModel(OsuFileScanner osuFileScanner, OsuDbInst osuDbInst)
    {
        _osuFileScanner = osuFileScanner;
        _osuDbInst = osuDbInst;
        ScannerViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FileScannerViewModel.IsScanning))
            {
                OnPropertyChanged(nameof(IsScanning));
            }
        };
    }

    [RelayCommand]
    private async Task BrowseDbAsync()
    {
        var path = await StoragePickerHelper.PickSingleFileAsync(@"请选择osu所在目录内的""osu!.db""", "osu!.db");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _osuDbInst.SyncOsuDbAsync(path, false);
            DbPath = path;
        }
        catch (Exception ex)
        {
            AppNotificationService.Instance.Push($"{I18NUtil.GetString(SRKeys.Err_Osudb_Sync)}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseCustomAsync()
    {
        var path = await StoragePickerHelper.PickFolderAsync("Select Folder");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            CustomSongsPath = path;
            await _osuFileScanner.CancelTaskAsync();
            await _osuFileScanner.NewScanAndAddAsync(path);
        }
        catch (Exception ex)
        {
            AppNotificationService.Instance.Push($"{I18NUtil.GetString(SRKeys.Err_Custom_Scan)}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        try
        {
            await _osuDbInst.SyncOsuDbAsync(DbPath, false);
            if (AppSettings.Default != null)
            {
                AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                AppSettings.SaveDefault();
            }
        }
        catch (Exception ex)
        {
            AppNotificationService.Instance.Push($"{I18NUtil.GetString(SRKeys.Err_Custom_Scan)}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ScanNowAsync()
    {
        await _osuFileScanner.CancelTaskAsync();
        await _osuFileScanner.NewScanAndAddAsync(CustomSongsPath);
    }

    [RelayCommand]
    private async Task CancelScanAsync()
    {
        await _osuFileScanner.CancelTaskAsync();
    }
}
