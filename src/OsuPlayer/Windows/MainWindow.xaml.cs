using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Coosu.Beatmap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
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
    public readonly LyricWindow LyricWindow;
    public ConfigWindow ConfigWindow;
    private bool _forceExit = false;

    private WindowState _lastState;

    private Task _searchLyricTask;

    private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private bool _disposed;
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new MainWindowViewModel();
        LyricWindow = new LyricWindow(this);

        Animation.Loaded += Animation_Loaded;
        PlayController.LikeClicked += Controller_LikeClicked;
        PlayController.ThumbClicked += Controller_ThumbClicked;
    }

    public void ForceClose()
    {
        _forceExit = true;
        Close();
    }

    private void Window_Initialized(object sender, EventArgs e)
    {
        _viewModel.IsNavigationCollapsed = AppSettings.Default.GeneralSection.IsNavigationCollapsed;
        if (AppSettings.Default.LyricSection.IsDesktopLyricEnabled)
        {
            LyricWindow.Show();
        }

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
            GetCurrentFirst<MiniWindow>()?.Close();
            Topmost = false;
        };

        BindHotKeyActions();
    }

    private void BindHotKeyActions()
    {
        var keyHookService = App.Current.ServiceProvider.GetService<KeyHookService>()!;
        keyHookService.InitializeAndActivateHotKeys();

        keyHookService.TogglePlayAction = () => _controller.PlayList.CurrentInfo?.TogglePlayHandle();
        keyHookService.PrevSongAction = async () => await _controller.PlayPrevAsync();
        keyHookService.NextSongAction = async () => await _controller.PlayNextAsync();
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
            if (LyricWindow.IsShown)
                LyricWindow.Hide();
            else
                LyricWindow.Show();
        };
    }

    /// <summary>
    /// Call lyric provider to check lyric
    /// </summary>
    public void SetLyricSynchronously()
    {
        if (!LyricWindow.IsVisible)
            return;

        Task.Run(async () =>
        {
            if (_searchLyricTask?.IsTaskBusy() == true)
            {
                await _searchLyricTask;
            }

            _searchLyricTask = Task.Run(async () =>
            {
                if (!_controller.IsPlayerReady) return;

                var lyricInst = Service.Get<LyricsService>();
                var meta = _controller.PlayList.CurrentInfo.OsuFile.Metadata;
                MetaString metaArtist = meta.ArtistMeta;
                MetaString metaTitle = meta.TitleMeta;
                var lyric = await lyricInst.LyricProvider.GetLyricAsync(metaArtist.ToUnicodeString(),
                    metaTitle.ToUnicodeString(), (int)_controller.Player.Duration.TotalMilliseconds);
                LyricWindow.SetNewLyric(lyric, metaArtist, metaTitle);
                LyricWindow.StartWork();
            });
        });
    }

    private void TriggerMiniWindow()
    {
        var mini = GetCurrentFirst<MiniWindow>();
        if (mini != null && !mini.IsClosed)
        {
            mini.Focus();
        }
        else
        {
            mini = new MiniWindow();
            mini.Show();
            Hide();
            SharedVm.Default.EnableVideo = false;
        }
    }

    #region Events

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        NotificationOverlay.ItemsSource = Notification.NotificationList;
        if (AppSettings.Default.GeneralSection.FirstOpen)
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
            //WelcomeControl.Show();
            //try
            //{
            //    await Service.Get<OsuDbInst>().LoadLocalDbAsync();
            //}
            //catch (Exception ex)
            //{
            //    Notification.Push(I18NUtil.GetString("err-mapNotInDb"), Title);
            //}

            try
            {
                await Service.Get<OsuFileScanningService>().NewScanAndAddAsync(AppSettings.Default.GeneralSection.CustomSongDir);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while scanning custom folder: {0}",
                    AppSettings.Default.GeneralSection.CustomSongDir);
                Notification.Push(I18NUtil.GetString("err-custom-scan"), Title);
            }
        }
        else
        {
            if (DateTime.Now - AppSettings.Default.LastTimeScanOsuDb > TimeSpan.FromDays(1))
            {
                try
                {
                    await Service.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Default.GeneralSection.DbPath, true);
                    AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                    AppSettings.SaveDefault();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while syncing osu!db: {0}", AppSettings.Default.GeneralSection.DbPath);
                    Notification.Push(I18NUtil.GetString("err-osudb-sync"), Title);
                }
            }
        }

        await UpdatePlayLists();

        _controller.LoadFinished += Controller_LoadFinished;

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
            Logger.Error(ex, "Error while checking for update");
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
                    Hide();
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
            GetCurrentFirst<MiniWindow>()?.Close();
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
        GetCurrentFirst<MiniWindow>()?.Close();
        LyricWindow.Dispose();
        NotifyIcon.Dispose();

        if (ConfigWindow != null && !ConfigWindow.IsClosed && ConfigWindow.IsInitialized)
        {
            ConfigWindow.Close();
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
        if (ConfigWindow == null || ConfigWindow.IsClosed)
        {
            ConfigWindow = new ConfigWindow();
            ConfigWindow.Show();
        }
        else
        {
            if (ConfigWindow.IsInitialized)
                ConfigWindow.Focus();
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

    private void Controller_LoadFinished(BeatmapContext arg1, CancellationToken arg2)
    {
        Execute.OnUiThread(() =>
        {
            /* Set Lyric */
            SetLyricSynchronously();
        });
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

    /// <summary>
    /// Update collections in the navigation bar.
    /// </summary>
    private async ValueTask UpdatePlayLists()
    {
        await using var dbContext = new ApplicationDbContext();
        var list = await dbContext.GetPlayListsAsync();
        _viewModel.PlayLists = new ObservableCollection<PlayList>(list);
    }
}