using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyAsio.Core.Audio;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Instances;
using OsuPlayer.Core.Scanning;
using OsuPlayer.Media.Audio;
using OsuPlayer.Presentation;
using OsuPlayer.Utils;
using OsuPlayer.Shared;
using OsuPlayer.Windows;

namespace OsuPlayer.ViewModels;

public partial class InterfacePageViewModel : ObservableObject
{
    public InterfaceSection InterfaceSettings => AppSettings.Default.Interface;

    public List<string> AvailableLanguages => I18NUtil.AvailableLangDic.Keys.ToList();

    private string _selectedLanguage;
    public string SelectedLanguage
    {
        get => _selectedLanguage ?? I18NUtil.CurrentLocale.Key;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value != null)
            {
                var locale = I18NUtil.AvailableLangDic[value];
                I18NUtil.SwitchToLang(locale);
                AppSettings.Default.Interface.Locale = locale;
                AppSettings.SaveDefault();
            }
        }
    }

    public bool MinimalMode
    {
        get => InterfaceSettings.MinimalMode;
        set
        {
            if (InterfaceSettings.MinimalMode != value)
            {
                InterfaceSettings.MinimalMode = value;
                OnPropertyChanged();
                AppSettings.SaveDefault();
            }
        }
    }
}

public partial class AboutPageViewModel : ObservableObject
{
    private readonly UpdateInst _updateInst;
    private readonly MainWindow _mainWindow;
    private readonly ConfigWindow _configWindow;
    private const string DtFormat = "g";

    [ObservableProperty]
    public partial string CurrentVersion { get; set; }

    [ObservableProperty]
    public partial string LastUpdateCheckText { get; set; }

    [ObservableProperty]
    public partial bool HasUpdate { get; set; }

    [ObservableProperty]
    public partial bool IsCheckingUpdate { get; set; }

    public AboutPageViewModel(UpdateInst updateInst)
    {
        _updateInst = updateInst;
        _mainWindow = WindowEx.GetCurrentFirst<MainWindow>();
        _configWindow = WindowEx.GetCurrentFirst<ConfigWindow>();

        CurrentVersion = _updateInst.CurrentVersion;
        HasUpdate = _updateInst.NewRelease != null;
        UpdateLastUpdateText();
    }

    private void UpdateLastUpdateText()
    {
        LastUpdateCheckText = AppSettings.Default.LastUpdateCheck == null
            ? I18NUtil.GetString("ui-sets-content-never")
            : AppSettings.Default.LastUpdateCheck.Value.ToString(DtFormat);
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        IsCheckingUpdate = true;
        try
        {
            bool? hasNew = await _updateInst.CheckUpdateAsync();
            AppSettings.Default.LastUpdateCheck = DateTime.Now;
            UpdateLastUpdateText();
            AppSettings.SaveDefault();

            if (hasNew == true)
            {
                HasUpdate = true;
                ShowNewVersionDialog();
            }
            else
            {
                MessageBox.Show(_configWindow, I18NUtil.GetString("ui-sets-content-alreadyNewest"), _configWindow.Title,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(_configWindow, I18NUtil.GetString("ui-sets-content-errorWhileCheckingUpdate") +
                                           Environment.NewLine +
                                           (ex.InnerException?.Message ?? ex.Message),
                _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private void ShowNewVersionDialog()
    {
        var newVersionWindow = new NewVersionWindow(_updateInst.NewRelease, _mainWindow);
        newVersionWindow.ShowDialog();
    }

    [RelayCommand]
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    [RelayCommand]
    private void ShowPrivacyPolicy()
    {
        MessageBox.Show("This software will NOT collect any user information.");
    }
}

public partial class GeneralPageViewModel : ObservableObject
{
    private readonly OsuFileScanner _osuFileScanner;
    private readonly OsuDbInst _osuDbInst;
    private readonly ConfigWindow _configWindow;
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public FileScannerViewModel ScannerViewModel => _osuFileScanner.ViewModel;

    public bool RunOnStartup
    {
        get => AppSettings.Default.General.RunOnStartup;
        set
        {
            if (AppSettings.Default.General.RunOnStartup != value)
            {
                AppSettings.Default.General.RunOnStartup = value;
                OnPropertyChanged();

                try
                {
                    using var rKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    if (value)
                    {
                        rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule?.FileName ?? "");
                    }
                    else
                    {
                        rKey?.DeleteValue("OsuPlayer", false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to set RunOnStartup registry key.");
                }

                AppSettings.SaveDefault();
            }
        }
    }

    public string DbPath
    {
        get => AppSettings.Default.General.DbPath;
        set
        {
            if (AppSettings.Default.General.DbPath != value)
            {
                AppSettings.Default.General.DbPath = value;
                OnPropertyChanged();
                AppSettings.SaveDefault();
            }
        }
    }

    public string CustomSongsPath
    {
        get => AppSettings.Default.General.CustomSongsPath;
        set
        {
            if (AppSettings.Default.General.CustomSongsPath != value)
            {
                AppSettings.Default.General.CustomSongsPath = value;
                OnPropertyChanged();
                AppSettings.SaveDefault();
            }
        }
    }

    public bool IsExitWhenClosed
    {
        get => AppSettings.Default.General.ExitWhenClosed == true;
        set
        {
            if (value && AppSettings.Default.General.ExitWhenClosed != true)
            {
                AppSettings.Default.General.ExitWhenClosed = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMinimizeWhenClosed));
                OnPropertyChanged(nameof(SetAsDefaultOptions));
                AppSettings.SaveDefault();
            }
        }
    }

    public bool IsMinimizeWhenClosed
    {
        get => AppSettings.Default.General.ExitWhenClosed == false;
        set
        {
            if (value && AppSettings.Default.General.ExitWhenClosed != false)
            {
                AppSettings.Default.General.ExitWhenClosed = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExitWhenClosed));
                OnPropertyChanged(nameof(SetAsDefaultOptions));
                AppSettings.SaveDefault();
            }
        }
    }

    public bool SetAsDefaultOptions
    {
        get => AppSettings.Default.General.ExitWhenClosed.HasValue;
        set
        {
            var current = AppSettings.Default.General.ExitWhenClosed.HasValue;
            if (current == value) return;

            if (value)
            {
                AppSettings.Default.General.ExitWhenClosed = IsExitWhenClosed;
            }
            else
            {
                AppSettings.Default.General.ExitWhenClosed = null;
            }

            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }

    public GeneralPageViewModel(OsuFileScanner osuFileScanner, OsuDbInst osuDbInst)
    {
        _osuFileScanner = osuFileScanner;
        _osuDbInst = osuDbInst;
        _configWindow = WindowEx.GetCurrentFirst<ConfigWindow>();
    }

    [RelayCommand]
    private async Task BrowseDbAsync()
    {
        var result = OsuDatabaseDialog.Browse(out var path);
        if (result == true)
        {
            try
            {
                await _osuDbInst.SyncOsuDbAsync(path, false);
                DbPath = path;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while syncing osu!db: {0}", path);
                MessageBox.Show(_configWindow, string.Format("{0}: {1}\r\n{2}",
                        I18NUtil.GetString("err-osudb-sync"), path, ex.Message),
                    _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        try
        {
            await _osuDbInst.SyncOsuDbAsync(AppSettings.Default.General.DbPath, false);
            AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
            AppSettings.SaveDefault();
        }
        catch (Exception ex)
        {
            var path = AppSettings.Default.General.DbPath;
            Logger.Error(ex, "Error while scanning custom folder: {0}", path);
            MessageBox.Show(_configWindow, string.Format("{0}: {1}\r\n{2}",
                    I18NUtil.GetString("err-custom-scan"), path, ex.Message),
                _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task BrowseCustomAsync()
    {
        using (var openFileDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Select Folder"
        })
        {
            var result = openFileDialog.ShowDialog();
            if (result == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                var path = openFileDialog.FileName;
                try
                {
                    CustomSongsPath = path;
                    await _osuFileScanner.CancelTaskAsync();
                    await _osuFileScanner.NewScanAndAddAsync(path);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while scanning custom folder: {0}", path);
                    MessageBox.Show(_configWindow, string.Format("{0}: {1}\r\n{2}",
                            I18NUtil.GetString("err-custom-scan"), path, ex.Message),
                        _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    [RelayCommand]
    private async Task ScanNowAsync()
    {
        await _osuFileScanner.CancelTaskAsync();
        await _osuFileScanner.NewScanAndAddAsync(AppSettings.Default.General.CustomSongsPath);
    }

    [RelayCommand]
    private async Task CancelScanAsync()
    {
        await _osuFileScanner.CancelTaskAsync();
    }
}

public partial class PlayPageViewModel : ObservableObject
{
    private readonly IAudioDeviceManager _audioDeviceManager;
    private readonly IPlaybackEngine _playbackEngine;
    private readonly ObservablePlayController _controller;
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private bool _isInitializing;

    public int GeneralOffset
    {
        get => AppSettings.Default.Play.GeneralOffset;
        set
        {
            if (AppSettings.Default.Play.GeneralOffset != value)
            {
                AppSettings.Default.Play.GeneralOffset = value;
                OnPropertyChanged();
                AppSettings.SaveDefault();
                if (_controller.Player != null)
                {
                    _controller.Player.GeneralOffset = AppSettings.Default.Play.GeneralActualOffset;
                }
            }
        }
    }

    public bool ReplacePlayList
    {
        get => AppSettings.Default.Play.ReplacePlayList;
        set
        {
            if (AppSettings.Default.Play.ReplacePlayList != value)
            {
                AppSettings.Default.Play.ReplacePlayList = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InsertPlayList));
                AppSettings.SaveDefault();
            }
        }
    }

    public bool InsertPlayList
    {
        get => !AppSettings.Default.Play.ReplacePlayList;
        set
        {
            if (AppSettings.Default.Play.ReplacePlayList != !value)
            {
                AppSettings.Default.Play.ReplacePlayList = !value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReplacePlayList));
                AppSettings.SaveDefault();
            }
        }
    }

    public bool AutoPlay
    {
        get => AppSettings.Default.Play.AutoPlay;
        set
        {
            if (AppSettings.Default.Play.AutoPlay != value)
            {
                AppSettings.Default.Play.AutoPlay = value;
                OnPropertyChanged();
                AppSettings.SaveDefault();
            }
        }
    }

    public bool Memory
    {
        get => AppSettings.Default.Play.Memory;
        set
        {
            if (AppSettings.Default.Play.Memory != value)
            {
                AppSettings.Default.Play.Memory = value;
                OnPropertyChanged();
                AppSettings.SaveDefault();
            }
        }
    }

    [ObservableProperty]
    public partial IReadOnlyList<DeviceDescription> AvailableDevices { get; set; }

    private DeviceDescription _selectedDevice;
    public DeviceDescription SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!SetProperty(ref _selectedDevice, value)) return;
            if (_isInitializing || value == null) return;

            var normalized = OsuPlayerAudioDevicePolicy.Normalize(value);
            ApplyFixedAudioDevicePolicy();
            AppSettings.Default.Play.DeviceDescription = OsuPlayerAudioDevicePolicy.ToConfiguration(normalized);
            AppSettings.SaveDefault();
            ApplyDeviceSettingsToEngine(normalized);
        }
    }

    public PlayPageViewModel(IAudioDeviceManager audioDeviceManager, IPlaybackEngine playbackEngine, ObservablePlayController controller)
    {
        _audioDeviceManager = audioDeviceManager;
        _playbackEngine = playbackEngine;
        _controller = controller;

        LoadDevices();
    }

    private void LoadDevices()
    {
        _isInitializing = true;
        try
        {
            ApplyFixedAudioDevicePolicy();
            var itemsSource = OsuPlayerAudioDevicePolicy.GetAvailableDevicesAsync(_audioDeviceManager)
                .GetAwaiter()
                .GetResult();
            AvailableDevices = itemsSource;
            var initial = OsuPlayerAudioDevicePolicy.SelectOrDefault(
                itemsSource,
                OsuPlayerAudioDevicePolicy.FromConfiguration(AppSettings.Default.Play.DeviceDescription));
            SelectedDevice = initial;
            AppSettings.Default.Play.DeviceDescription = OsuPlayerAudioDevicePolicy.ToConfiguration(initial);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private static void ApplyFixedAudioDevicePolicy()
    {
        AppSettings.Default.Play.DesiredLatency = OsuPlayerAudioDevicePolicy.FixedLatency;
        AppSettings.Default.Play.IsExclusive = OsuPlayerAudioDevicePolicy.UseExclusiveMode;
        AppSettings.Default.Play.DeviceDescription =
            OsuPlayerAudioDevicePolicy.ToConfiguration(
                OsuPlayerAudioDevicePolicy.FromConfiguration(AppSettings.Default.Play.DeviceDescription));
    }

    private void ApplyDeviceSettingsToEngine(DeviceDescription deviceDescription)
    {
        if (deviceDescription == null) return;
        try
        {
            OsuPlayerAudioDevicePolicy.StartDevice(_playbackEngine, deviceDescription);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error while applying audio device settings.");
        }
    }
}
