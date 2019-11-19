using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Control.Notification;
using Milky.OsuPlayer.Instances;

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
        public bool ForceExit = false;

        private WindowState _lastState;

        private readonly BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();
        private readonly AppDbOperator _appDbOperator = new AppDbOperator();

        private Task _searchLyricTask;

        public MainWindow()
        {
            PlayerViewModel.InitViewModel();

            InitializeComponent();
            ViewModel = (MainWindowViewModel)DataContext;
            ViewModel.Player = PlayerViewModel.Current;
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
                PlayerViewModel.Current.EnableVideo = true;
                GetCurrentFirst<MiniWindow>()?.Close();
            };
            TryBindHotKeys();
        }

        private void TryBindHotKeys()
        {
            var page = new Pages.Settings.HotKeyPage();
            OverallKeyHook.AddKeyHook(page.PlayPause.Name, () => { PlayController.Default.TogglePlay(); });
            OverallKeyHook.AddKeyHook(page.Previous.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.Next.Name, async () => { await PlayController.Default.PlayNext(); });
            OverallKeyHook.AddKeyHook(page.VolumeUp.Name, () => { AppSettings.Default.Volume.Main += 0.05f; });
            OverallKeyHook.AddKeyHook(page.VolumeDown.Name, () => { AppSettings.Default.Volume.Main -= 0.05f; });
            OverallKeyHook.AddKeyHook(page.FullMini.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.AddToFav.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.Lyric.Name, () =>
            {
                if (LyricWindow.IsShown)
                    LyricWindow.Hide();
                else
                    LyricWindow.Show();
            });
        }

        private void WindowBase_Deactivated(object sender, EventArgs e)
        {
            PlayController.Default.PopPlayList.IsOpen = false;
        }

        private void ButtonBase_Click(object sender, RoutedEventArgs e)
        {
            PlayController.Default.PopPlayList.IsOpen = false;
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

        private bool IsMapFavorite(MapInfo info)
        {
            var album = _appDbOperator.GetCollectionsByMap(info);
            bool isFavorite = album != null && album.Any(k => k.LockedBool);

            return isFavorite;
        }

        private bool IsMapFavorite(MapIdentity identity)
        {
            var info = _appDbOperator.GetMapFromDb(identity);
            return IsMapFavorite(info);
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
                    var player = Services.Get<PlayersInst>().AudioPlayer;
                    if (player == null)
                        return;

                    var meta = player.OsuFile.Metadata;
                    var lyricInst = Services.Get<LyricsInst>();
                    var lyric = await lyricInst.LyricProvider.GetLyricAsync(meta.ArtistMeta.ToUnicodeString(),
                        meta.TitleMeta.ToUnicodeString(), player.Duration);
                    LyricWindow.SetNewLyric(lyric, player.OsuFile);
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
                PlayerViewModel.Current.EnableVideo = false;
            }
        }

        #region Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.NotificationList = new ObservableCollection<NotificationOption>();
            NotificationOverlay.ItemsSource = App.NotificationList;
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
            }
            else
            {
                await Services.Get<OsuFileScanner>().NewScanAndAddAsync(AppSettings.Default.General.CustomSongsPath);
                if (DateTime.Now - AppSettings.Default.LastTimeScanOsuDb > TimeSpan.FromDays(1))
                {
                    await Services.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Default.General.DbPath, true);
                    AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                    AppSettings.SaveDefault();
                }
            }

            UpdateCollections();

            PlayController.Default.OnNewFileLoaded += Controller_OnNewFileLoaded;
            PlayController.Default.OnLikeClick += Controller_OnLikeClick;
            PlayController.Default.OnThumbClick += Controller_OnThumbClick;

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
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AppSettings.Default.General.ExitWhenClosed == null && !ForceExit)
            {
                e.Cancel = true;
                var closingControl = new ClosingControl();
                FrontDialogOverlay.ShowContent(closingControl, new FrontDialogOverlay.ShowContentOptions
                {
                    Width = 280,
                    Height = 180,
                    Title = "关闭确认"
                }, (obj, arg) =>
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
            PlayController.Default?.Dispose();
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

        private void Animation_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSettings.Default.CurrentPath == null || !AppSettings.Default.Play.Memory)
            {
                return;
            }

            Animation.StartScene(async () =>
            {
                // 加至播放列表
                var entries = _beatmapDbOperator.GetBeatmapsByIdentifiable(AppSettings.Default.CurrentList);

                await Services.Get<PlayerList>()
                    .RefreshPlayListAsync(PlayerList.FreshType.All, beatmaps: entries);

                bool play = AppSettings.Default.Play.AutoPlay;
                await PlayController.Default.PlayNewFile(AppSettings.Default.CurrentPath, play);
            });
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

        private void Controller_OnNewFileLoaded(object sender, HandledEventArgs e)
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
            var id = Services.Get<PlayerList>().CurrentIdentity;
            var entry = _beatmapDbOperator.GetBeatmapByIdentifiable(id);
            if (entry == null)
            {
                Notification.Show("该图不存在于该osu!db中", Title);
                return;
            }

            //if (!ViewModel.IsMiniMode)
            FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(entry),
                DialogOptionFactory.SelectCollectionOptions);
            //FramePop.Navigate(new SelectCollectionPage(entry));
            //else
            //{
            //    var collection = _appDbOperator.GetCollections().First(k => k.Locked);
            //    if (Services.Get<PlayerList>().CurrentInfo.IsFavorite)
            //    {
            //        _appDbOperator.RemoveMapFromCollection(entry, collection);
            //        Services.Get<PlayerList>().CurrentInfo.IsFavorite = false;
            //    }
            //    else
            //    {
            //        await SelectCollectionPage.AddToCollectionAsync(collection, new[] { entry });
            //        Services.Get<PlayerList>().CurrentInfo.IsFavorite = true;
            //    }
            //}

            //IsMapFavorite(Services.Get<PlayerList>().CurrentInfo.Identity);
        }

        #endregion Events
    }
}