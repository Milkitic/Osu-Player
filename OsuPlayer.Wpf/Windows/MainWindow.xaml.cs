using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.ViewModels;
using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowEx
    {
        internal MainWindowViewModel ViewModel { get; }

        public readonly LyricWindow LyricWindow;
        public ConfigWindow ConfigWindow;
        public readonly OverallKeyHook OverallKeyHook;
        private bool _forceExit = false;

        private WindowState _lastState;

        private readonly AppDbOperator _appDbOperator = new AppDbOperator();

        private Task _searchLyricTask;

        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private bool _disposed;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = (MainWindowViewModel)DataContext;
            LyricWindow = new LyricWindow(this);
            if (AppSettings.Default.Lyric.EnableLyric)
                LyricWindow.Show();

            OverallKeyHook = new OverallKeyHook(this);
            Animation.Loaded += Animation_Loaded;
            PlayController.LikeClicked += Controller_LikeClicked;
            PlayController.ThumbClicked += Controller_ThumbClicked;
            MiniPlayController.CloseButtonClicked += () =>
            {
                if (AppSettings.Default.General.ExitWhenClosed == null) Show();
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
            TryBindHotKeys();
        }

        private void TryBindHotKeys()
        {
            OverallKeyHook.AddKeyHook(HotKeyType.TogglePlay, () => _controller.PlayList.CurrentInfo?.TogglePlayHandle());
            OverallKeyHook.AddKeyHook(HotKeyType.PrevSong, async () => await _controller.PlayPrevAsync());
            OverallKeyHook.AddKeyHook(HotKeyType.NextSong, async () => await _controller.PlayNextAsync());
            OverallKeyHook.AddKeyHook(HotKeyType.VolumeUp, () =>
            {
                AppSettings.Default.Volume.Main += 0.05f;
                AppSettings.SaveDefault();
            });
            OverallKeyHook.AddKeyHook(HotKeyType.VolumeDown, () =>
            {
                AppSettings.Default.Volume.Main -= 0.05f;
                AppSettings.SaveDefault();
            });
            OverallKeyHook.AddKeyHook(HotKeyType.SwitchFullMiniMode, () => { TriggerMiniWindow(); });
            OverallKeyHook.AddKeyHook(HotKeyType.AddCurrentToFav, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(HotKeyType.SwitchLyricWindow, () =>
            {
                if (LyricWindow.IsShown)
                    LyricWindow.Hide();
                else
                    LyricWindow.Show();
            });
        }

        /// <summary>
        /// Update collections in the navigation bar.
        /// </summary>
        public void UpdateCollections()
        {
            var list = _appDbOperator.GetCollections();
            list.Reverse();
            ViewModel.Collection = new ObservableCollection<Collection>(list);
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
                    await Task.WhenAny(_searchLyricTask);

                _searchLyricTask = Task.Run(async () =>
                {
                    if (!_controller.IsPlayerReady) return;

                    var lyricInst = Service.Get<LyricsInst>();
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
            if (AppSettings.Default.General.FirstOpen)
            {
                FrontDialogOverlay.ShowContent(new WelcomeControl(), new FrontDialogOverlay.ShowContentOptions
                {
                    Height = 400,
                    Width = 350,
                    ShowDialogButtons = false,
                    ShowTitleBar = false
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
                    await Service.Get<OsuFileScanner>().NewScanAndAddAsync(AppSettings.Default.General.CustomSongsPath);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while scanning custom folder: {0}",
                        AppSettings.Default.General.CustomSongsPath);
                    Notification.Push(I18NUtil.GetString("err-custom-scan"), Title);
                }
            }
            else
            {
                if (DateTime.Now - AppSettings.Default.LastTimeScanOsuDb > TimeSpan.FromDays(1))
                {
                    try
                    {
                        await Service.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Default.General.DbPath, true);
                        AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                        AppSettings.SaveDefault();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error while syncing osu!db: {0}", AppSettings.Default.General.DbPath);
                        Notification.Push(I18NUtil.GetString("err-osudb-sync"), Title);
                    }
                }
            }

            UpdateCollections();

            _controller.LoadFinished += Controller_LoadFinished;

            try
            {
                var updater = Service.Get<UpdateInst>();
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
                Notification.Push(I18NUtil.GetString("err-update-check"), Title);
            }
        }

        /// <summary>
        /// Clear things.
        /// </summary>
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AppSettings.Default.General.ExitWhenClosed == null && !_forceExit)
            {
                e.Cancel = true;
                var closingControl = new ClosingControl();
                FrontDialogOverlay.ShowContent(closingControl, DialogOptionFactory.ClosingOptions, (obj, arg) =>
                {
                    if (closingControl.AsDefault.IsChecked == true)
                    {
                        AppSettings.Default.General.ExitWhenClosed = closingControl.RadioMinimum.IsChecked != true;
                        AppSettings.SaveDefault();
                    }

                    if (closingControl.RadioMinimum.IsChecked == true)
                        Hide();
                    else
                    {
                        _forceExit = true;
                        Close();
                    }
                });

                return;
            }

            if (AppSettings.Default.General.ExitWhenClosed == false && !_forceExit)
            {
                WindowState = WindowState.Minimized;
                GetCurrentFirst<MiniWindow>()?.Close();
                Hide();
                e.Cancel = true;
                return;
            }

            if (_disposed) return;
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
            if (AppSettings.Default.CurrentMap == null || !AppSettings.Default.Play.Memory)
                return;

            // 加至播放列表
            var entries = _appDbOperator.GetBeatmapsByIdentifiable(AppSettings.Default.CurrentList);

            await _controller.PlayList.SetSongListAsync(entries, true, false, false);

            bool play = AppSettings.Default.Play.AutoPlay;
            if (AppSettings.Default.CurrentMap.IsMapTemporary())
            {
                await _controller.PlayNewAsync(AppSettings.Default.CurrentMap.Value.FolderName, play);
            }
            else
            {
                var current = _appDbOperator.GetBeatmapByIdentifiable(AppSettings.Default.CurrentMap);
                await _controller.PlayNewAsync(current, play);
            }
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            var addCollectionControl = new AddCollectionControl();
            FrontDialogOverlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, (obj, args) =>
            {
                _appDbOperator.AddCollection(addCollectionControl.CollectionName.Text);
                UpdateCollections();
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
            var detail = _controller.PlayList.CurrentInfo.Beatmap;
            var entry = _appDbOperator.GetBeatmapByIdentifiable(detail.GetIdentity());
            if (entry == null)
            {
                Notification.Push(I18NUtil.GetString("err-mapNotInDb"), Title);
                return;
            }

            UiComponents.FrontDialogComponent.FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(entry),
                DialogOptionFactory.SelectCollectionOptions);
        }

        #endregion Events

        public void ForceClose()
        {
            _forceExit = true;
            Close();
        }
    }
}