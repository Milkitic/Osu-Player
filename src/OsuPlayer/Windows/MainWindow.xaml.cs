using System.Windows;
using Anotar.NLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Audio.Mixing;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.UserControls;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Windows;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : WindowEx
{
    private readonly PlayerService _playerService;
    private readonly PlayListService _playListService;

    private ConfigWindow _configWindow;
    private MiniWindow _miniWindow;
    private bool _forceExit = false;

    private WindowState _lastState;

    private bool _disposed;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        _playerService = App.Current.ServiceProvider.GetService<PlayerService>();
        _playListService = App.Current.ServiceProvider.GetService<PlayListService>();
        InitializeComponent();
        DataContext = _viewModel = new MainWindowViewModel();
        Animation.Loaded += Animation_Loaded;
        PlayController.LikeClicked += Controller_LikeClicked;
        PlayController.ThumbClicked += Controller_ThumbClicked;
    }

    public void ForceClose()
    {
        _forceExit = true;
        Close();
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
            AppSettings.Default.VolumeSection.Main += 0.05f;
            AppSettings.SaveDefault();
        };
        keyHookService.VolumeDownAction = () =>
        {
            AppSettings.Default.VolumeSection.Main -= 0.05f;
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
        await using var dbContext = App.Current.ServiceProvider.GetService<ApplicationDbContext>()!;
        var softwareState = await dbContext.GetSoftwareState();

        _viewModel.IsNavigationCollapsed = !softwareState.ShowFullNavigation;

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
            FrontDialogOverlay.ShowContent(new WelcomeControl(), new FrontDialogOverlay.ShowContentOptions
            {
                Height = 400,
                Width = 350,
                ShowDialogButtons = false,
                ShowTitleBar = false
            }, (obj, args) =>
            {
                SwitchSearch.IsChecked = true;
            });

            var customSongDir = AppSettings.Default.GeneralSection.CustomSongDir;
            try
            {
                await Service.Get<OsuFileScanningService>().NewScanAndAddAsync(customSongDir);
            }
            catch (Exception ex)
            {
                LogTo.ErrorException($"Error while scanning custom folder: {customSongDir}", ex);
                Notification.Push(I18NUtil.GetString("err-custom-scan"), Title);
            }
        }
        else
        {
            if (DateTime.Now - softwareState.LastSync > TimeSpan.FromDays(1))
            {
                var dbPath = AppSettings.Default.GeneralSection.DbPath;
                try
                {
                    await Service.Get<OsuDbInst>().SyncOsuDbAsync(dbPath, true);
                }
                catch (Exception ex)
                {
                    LogTo.ErrorException($"Error while syncing osu!db: {dbPath}", ex);
                    Notification.Push(I18NUtil.GetString("err-osudb-sync"), Title);
                    return;
                }

                softwareState.LastSync = DateTime.Now;
                await dbContext.UpdateAndSaveChangesAsync(softwareState, k => k.LastSync);
            }
        }

        await SharedVm.Default.UpdatePlayLists();

        try
        {
            var updater = Service.Get<UpdateService>();
            bool? hasUpdate = await updater.CheckUpdateAsync();
            if (hasUpdate == true && updater.NewRelease.NewVerString != AppSettings.Default.IgnoredVer)
            {
                var newVersionWindow = new NewVersionWindow(updater.NewRelease, this);
                newVersionWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while checking for update", ex);
            Notification.Push(I18NUtil.GetString("err-update-check") + $": {ex.Message}", Title);
        }
    }

    /// <summary>
    /// Clear things.
    /// </summary>
    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (AppSettings.Default.GeneralSection.ExitWhenClosed == null && !_forceExit)
        {
            e.Cancel = true;
            var closingControl = new ClosingControl();
            FrontDialogOverlay.ShowContent(closingControl, DialogOptionFactory.ClosingOptions, (obj, arg) =>
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

            return;
        }

        if (AppSettings.Default.GeneralSection.ExitWhenClosed == false && !_forceExit)
        {
            WindowState = WindowState.Minimized;
            _miniWindow?.Close();
            Hide();
            e.Cancel = true;
            return;
        }

        if (_disposed)
        {
            Application.Current.Shutdown();
            return;
        }

        e.Cancel = true;
        _miniWindow?.Close();
        LyricWindow.Dispose();
        NotifyIcon.Dispose();

        if (_configWindow != null && !_configWindow.IsClosed && _configWindow.IsInitialized)
        {
            _configWindow.Close();
        }

        if (_controller != null) await _controller.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
        await Task.CompletedTask.ConfigureAwait(false);
        Execute.ToUiThread(ForceClose);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            return;
        _lastState = WindowState;
    }

    private async void Animation_Loaded(object sender, RoutedEventArgs e)
    {
        await using var appDbContext = new ApplicationDbContext();
        var lastPlay = await appDbContext.RecentList.OrderByDescending(k => k.PlayTime).FirstOrDefaultAsync();
        if (lastPlay == null || !AppSettings.Default.PlaySection.Memory)
            return;

        // 加至播放列表
        var beatmaps = await appDbContext.Playlist
            .OrderBy(k => k.Id)
            .Include(k => k.Beatmap)
            .Select(k => k.Beatmap)
            .ToListAsync();

        var lastBeatmap = await appDbContext.Playlist
            .OrderByDescending(k => k.PlayTime)
            .Include(k => k.Beatmap)
            .Select(k => k.Beatmap)
            .FirstOrDefaultAsync();

        await _controller.PlayList.SetSongListAsync(beatmaps, true, false, false);

        bool play = AppSettings.Default.PlaySection.AutoPlay;
        if (lastBeatmap.IsTemporary)
        {
            await _controller.PlayNewAsync(lastBeatmap.FolderNameOrPath, play);
        }
        else
        {
            await _controller.PlayNewAsync(lastBeatmap, play);
        }
    }

    private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
    {
        var addCollectionControl = new AddCollectionControl();
        FrontDialogOverlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, async (obj, args) =>
        {
            await using var dbContext = new ApplicationDbContext();
            await dbContext.AddCollection(addCollectionControl.CollectionName.Text); //todo: exists
            await UpdatePlayLists();
        });
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
        var mini = GetCurrentFirst<MiniWindow>();
        if (mini != null)
        {
            mini.Focus();
            return;
        }

        Topmost = true;
        Topmost = false;
        Show();
        WindowState = _lastState;
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        _forceExit = true;
        this.Close();
    }

    private void MenuConfig_Click(object sender, RoutedEventArgs e)
    {
        BtnSettings_Click(sender, e);
    }

    private void MenuOpenHideLyric_Click(object sender, RoutedEventArgs e)
    {
        if (LyricWindow.IsShown)
        {
            LyricWindow.Hide();
        }
        else
        {
            LyricWindow.Show();
        }
    }

    private void MenuLockLyric_Click(object sender, RoutedEventArgs e)
    {
        LyricWindow.IsLocked = !LyricWindow.IsLocked;
    }

    private void Controller_ThumbClicked(object sender, RoutedEventArgs e)
    {
        MainFrame.Content = null;
    }

    private void Controller_LikeClicked(object sender, RoutedEventArgs e)
    {
        if (_controller.PlayList.CurrentInfo == null) return;
        var beatmap = _controller.PlayList.CurrentInfo.Beatmap;

        FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(beatmap),
            DialogOptionFactory.SelectCollectionOptions);
    }

    private void BtnNavigationTrigger_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.IsNavigationCollapsed = !_viewModel.IsNavigationCollapsed;
        // Todo: save to db
    }

    #endregion Events
}