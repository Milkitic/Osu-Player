using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
using OSharp.Beatmap;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        internal MainWindowViewModel ViewModel { get; }

        public readonly LyricWindow LyricWindow;
        public ConfigWindow ConfigWindow;
        public readonly OverallKeyHook OverallKeyHook;
        private bool ForceExit = false;

        private WindowState _lastState;

        private readonly AppDbOperator _appDbOperator = new AppDbOperator();

        private Task _searchLyricTask;

        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = (MainWindowViewModel)DataContext;
            LyricWindow = new LyricWindow(this);
            if (AppSettings.Default.Lyric.EnableLyric)
                LyricWindow.Show();

            OverallKeyHook = new OverallKeyHook(this);
            Animation.Loaded += Animation_Loaded;
            MiniPlayController.CloseButtonClicked += () =>
            {
                if (AppSettings.Default.General.ExitWhenClosed == null) Show();
                Close();
            };
            MiniPlayController.MaxButtonClicked += () =>
            {
                Topmost = true;
                Topmost = false;
                Show();
                SharedVm.Default.EnableVideo = true;
                GetCurrentFirst<MiniWindow>()?.Close();
            };
            TryBindHotKeys();
        }

        private void TryBindHotKeys()
        {
            OverallKeyHook.AddKeyHook(HotKeyType.TogglePlay, () => { _controller.Player.TogglePlay(); });
            OverallKeyHook.AddKeyHook(HotKeyType.PrevSong, async () => { await _controller.PlayPrevAsync(); });
            OverallKeyHook.AddKeyHook(HotKeyType.NextSong, async () => { await _controller.PlayNextAsync(); });
            OverallKeyHook.AddKeyHook(HotKeyType.VolumeUp, () => { AppSettings.Default.Volume.Main += 0.05f; });
            OverallKeyHook.AddKeyHook(HotKeyType.VolumeDown, () => { AppSettings.Default.Volume.Main -= 0.05f; });
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

                    var lyricInst = Services.Get<LyricsInst>();
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
                await Services.Get<OsuDbInst>().LoadLocalDbAsync();
                await Services.Get<OsuFileScanner>().NewScanAndAddAsync(AppSettings.Default.General.CustomSongsPath);
            }
            else
            {
                if (DateTime.Now - AppSettings.Default.LastTimeScanOsuDb > TimeSpan.FromDays(1))
                {
                    await Services.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Default.General.DbPath, true);
                    AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                    AppSettings.SaveDefault();
                }
            }

            UpdateCollections();

            _controller.LoadFinished += Controller_LoadFinished;
            PlayController.OnLikeClick += Controller_OnLikeClick;
            PlayController.OnThumbClick += Controller_OnThumbClick;

            var updater = Services.Get<Updater>();
            bool? hasUpdate = await updater.CheckUpdateAsync();
            if (hasUpdate == true && updater.NewRelease.NewVerString != AppSettings.Default.IgnoredVer)
            {
                var newVersionWindow = new NewVersionWindow(updater.NewRelease, this);
                newVersionWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Clear things.
        /// </summary>
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AppSettings.Default.General.ExitWhenClosed == null && !ForceExit)
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
                        ForceExit = true;
                        Close();
                    }
                });

                return;
            }

            if (AppSettings.Default.General.ExitWhenClosed == false && !ForceExit)
            {
                WindowState = WindowState.Minimized;
                GetCurrentFirst<MiniWindow>()?.Close();
                Hide();
                e.Cancel = true;
                return;
            }

            GetCurrentFirst<MiniWindow>()?.Close();
            await _controller?.DisposeAsync();
            LyricWindow.Dispose();
            NotifyIcon.Dispose();
            if (ConfigWindow != null && !ConfigWindow.IsClosed && ConfigWindow.IsInitialized)
            {
                ConfigWindow.Close();
            }
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
            var current = _appDbOperator.GetBeatmapByIdentifiable(AppSettings.Default.CurrentMap);
            await _controller.PlayNewAsync(current, play);
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
            ForceExit = true;
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

        private void Controller_OnThumbClick(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = null;
        }

        private void Controller_OnLikeClick(object sender, RoutedEventArgs e)
        {
            var detail = _controller.PlayList.CurrentInfo.Beatmap;
            var entry = _appDbOperator.GetBeatmapByIdentifiable(detail.GetIdentity());
            if (entry == null)
            {
                Notification.Push("该图不存在于该osu!db中", Title);
                return;
            }

            FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(entry),
                DialogOptionFactory.SelectCollectionOptions);
        }

        #endregion Events

        public void ForceClose()
        {
            ForceExit = true;
            Close();
        }
    }
}