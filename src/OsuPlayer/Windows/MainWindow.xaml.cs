using System.Windows;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.ViewModels;

namespace Milki.OsuPlayer.Windows;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowBase
{
    private readonly UpdateService _updateService;
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;
    private readonly BeatmapSyncService _syncService;
    private readonly OsuFileScanningService _osuFileScanningService;
    private readonly LyricsService _lyricsService;

    private ConfigWindow _configWindow;
    private MiniWindow _miniWindow;

    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        _syncService = ServiceProviders.Default.GetService<BeatmapSyncService>();
        _osuFileScanningService = ServiceProviders.Default.GetService<OsuFileScanningService>()!;
        _lyricsService = ServiceProviders.Default.GetService<LyricsService>()!;
        _updateService = ServiceProviders.Default.GetService<UpdateService>()!;
        DataContext = _viewModel = new MainWindowViewModel();

        InitializeComponent();
        Animation.Loaded += Animation_Loaded;
        PlayController.LikeClicked += Controller_LikeClicked;
        PlayController.ThumbClicked += Controller_ThumbClicked;
    }

    protected override async Task<bool> OnAsyncClosing()
    {
        var promptType = AppSettings.Default.GeneralSection.ExitWhenClosed;
        if (IsForceExitState || promptType == true)
        {
            _miniWindow?.Close();
            _lyricsService.Dispose();
            NotifyIcon.Dispose();

            if (_configWindow is { IsClosed: false, IsInitialized: true })
            {
                _configWindow.Close();
            }

            if (_playerService != null)
            {
                await _playerService.DisposeAsync();
            }

            Application.Current.Shutdown();
            return true;
        }

        if (promptType == null)
        {
            var closingControl = new ClosingControl();
            ContentDialog.ShowContent(closingControl, DialogOptionFactory.ClosingOptions, (obj, arg) =>
            {
                if (closingControl.AsDefault.IsChecked == true)
                {
                    AppSettings.Default.GeneralSection.ExitWhenClosed = closingControl.RadioMinimum.IsChecked != true;
                    AppSettings.SaveDefault();
                }

                if (closingControl.RadioMinimum.IsChecked == true)
                {
                    Hide();
                }
                else
                {
                    ForceClose();
                }
            });

            return false;
        }

        // promptType == false
        WindowState = WindowState.Minimized;
        _miniWindow?.Close();
        Hide();
        return false;
    }

    private void BindHotKeyActions()
    {
        var keyHookService = App.Current.ServiceProvider.GetService<KeyHookService>()!;
        keyHookService.InitializeAndActivateHotKeys();

        keyHookService.TogglePlayAction = async () => await _playerService.TogglePlayAsync();
        keyHookService.PrevSongAction = async () => await _playerService.PlayPreviousAsync();
        keyHookService.NextSongAction = async () => await _playerService.PlayNextAsync();
        keyHookService.VolumeUpAction = () =>
        {
            AppSettings.Default.VolumeSection.MainVolume += 0.05f;
            AppSettings.SaveDefault();
        };
        keyHookService.VolumeDownAction = () =>
        {
            AppSettings.Default.VolumeSection.MainVolume -= 0.05f;
            AppSettings.SaveDefault();
        };
        keyHookService.SwitchFullMiniModeAction = () =>
        {
            TriggerMiniWindow();
        };
        keyHookService.AddCurrentToFavAction = () =>
        {
            //TODO
        };
        keyHookService.SwitchLyricWindowAction = () =>
        {
            SharedVm.Default.IsLyricWindowEnabled = !SharedVm.Default.IsLyricWindowEnabled;
        };
    }

    private void TriggerMiniWindow()
    {
        if (_miniWindow is { IsClosed: false })
        {
            ProcessUtils.ShowWindow(_miniWindow.Handle, ProcessUtils.SW_SHOW);
            ProcessUtils.SwitchToThisWindow(_miniWindow.Handle, true);
            _miniWindow.Activate();
            _miniWindow.Focus();
        }
        else
        {
            _miniWindow = new MiniWindow();
            _miniWindow.Show();
            Hide();
            SharedVm.Default.EnableVideo = false;
        }
    }

    #region Events

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await using var applicationDbContext = ServiceProviders.GetApplicationDbContext();
        var softwareState = await applicationDbContext.GetSoftwareState();

        SharedVm.Default.IsNavigationCollapsed = !softwareState.ShowFullNavigation;
        MiniPlayController.CloseButtonClicked += () =>
        {
            if (AppSettings.Default.GeneralSection.ExitWhenClosed == null) Show();
            Close();
        };

        MiniPlayController.MaxButtonClicked += () =>
        {
            Topmost = true;
            Show();
            SharedVm.Default.EnableVideo = true;
            _miniWindow?.Close();
            Topmost = false;
        };

        BindHotKeyActions();

        NotificationOverlay.ItemsSource = Notification.NotificationList;
        if (softwareState.ShowWelcome)
        {
            ContentDialog.ShowContent(new WelcomeControl(), new ContentDialog.DialogOptions
            {
                Height = 400,
                Width = 350,
                ShowDialogButtons = false,
                ShowTitleBar = false
            }, (obj, args) =>
            {
            });

            var songDir = AppSettings.Default.GeneralSection.CustomSongDir;
            try
            {
                await _osuFileScanningService.SyncCustomFolderAsync(songDir);
            }
            catch (Exception ex)
            {
                Notification.Push($"{I18NUtil.GetString("err-custom-scan")}: {songDir}\r\n{ex.Message}", Title);
            }
        }
        else
        {
            if (DateTime.Now - softwareState.LastSync > TimeSpan.FromDays(1))
            {
                var dbPath = AppSettings.Default.GeneralSection.DbPath;
                if (dbPath != null)
                {
                    try
                    {
                        await _syncService.SyncOsuDbAsync(dbPath);
                    }
                    catch (Exception ex)
                    {
                        Notification.Push(I18NUtil.GetString("err-osudb-sync"), Title);
                    }
                }
            }
        }

        await SharedVm.Default.UpdatePlayListsAsync();

        try
        {
            bool? hasUpdate = await _updateService.CheckUpdateAsync();
            if (hasUpdate == true && _updateService.NewRelease!.NewVerString != softwareState.IgnoredVersion)
            {
                var newVersionWindow = new NewVersionWindow(_updateService.NewRelease, this);
                newVersionWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while checking for update", ex);
            Notification.Push(I18NUtil.GetString("err-update-check") + $": {ex.Message}", Title);
        }
    }

    private async void Animation_Loaded(object sender, RoutedEventArgs e)
    {
        await using var applicationDbContext = ServiceProviders.GetApplicationDbContext();
        var lastPlay = (await applicationDbContext.GetRecentListFull(0, 1)).Results.FirstOrDefault();
        var currentPlays = await applicationDbContext.GetCurrentListFull();
        if (lastPlay?.IsItemLost == false)
        {
            lastPlay = currentPlays.LastOrDefault(k => !k.IsItemLost && k.PlayItemId == lastPlay.PlayItemId) ??
                       currentPlays.LastOrDefault(k => !k.IsItemLost) ??
                       lastPlay;
        }
        else
        {
            lastPlay = currentPlays.LastOrDefault(k => !k.IsItemLost);
        }

        _playListService.SetPathList(
            currentPlays.Where(k => k.PlayItem != null).Select(k => k.PlayItem.StandardizedPath), false);

        if (lastPlay != null)
        {
            await _playerService.InitializeNewAsync(lastPlay.PlayItem!.StandardizedPath,
                AppSettings.Default.PlaySection.AutoPlay);
        }
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_configWindow == null || _configWindow.IsClosed)
        {
            _configWindow = new ConfigWindow();
            _configWindow.Show();
        }
        else
        {
            if (_configWindow.IsInitialized)
                _configWindow.Focus();
        }
    }

    private void BtnMini_Click(object sender, RoutedEventArgs e)
    {
        TriggerMiniWindow();
    }

    private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        var miniWindow = App.Current.Windows.OfType<MiniWindow>().FirstOrDefault();
        if (miniWindow != null)
        {
            miniWindow.Focus();
            return;
        }

        Topmost = true;
        Topmost = false;
        Show();
        WindowState = LastWindowsState;
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        ForceClose();
    }

    private void MenuConfig_Click(object sender, RoutedEventArgs e)
    {
        BtnSettings_Click(sender, e);
    }

    private void MenuOpenHideLyric_Click(object sender, RoutedEventArgs e)
    {
        SharedVm.Default.IsLyricWindowEnabled = !SharedVm.Default.IsLyricWindowEnabled;
    }

    private void MenuLockLyric_Click(object sender, RoutedEventArgs e)
    {
        SharedVm.Default.IsLyricWindowLocked = !SharedVm.Default.IsLyricWindowLocked;
    }

    private void Controller_ThumbClicked(object sender, RoutedEventArgs e)
    {
        MainFrame.Content = null;
    }

    private void Controller_LikeClicked(object sender, RoutedEventArgs e)
    {
        if (_playerService.LastLoadContext is { PlayItem: { } playItem })
        {
            ContentDialog.ShowContent(new SelectPlayListControl(playItem),
                DialogOptionFactory.SelectPlayListOptions);
        }
    }

    #endregion Events
}