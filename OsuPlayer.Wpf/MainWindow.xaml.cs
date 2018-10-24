using DMSkin.WPF;
using Microsoft.Win32;
using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Media;
using Milkitic.OsuPlayer.Media.Music;
using Milkitic.OsuPlayer.Pages;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Collection = Milkitic.OsuPlayer.Data.Collection;

namespace Milkitic.OsuPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : DMSkinSimpleWindow
    {
        public PageParts Pages => new PageParts
        {
            SearchPage = new SearchPage(this),
            RecentPlayPage = new RecentPlayPage(this),
            FindPage = new FindPage(this),
            StoryboardPage = new StoryboardPage(this),
            ExportPage = new ExportPage(this),
        };

        public readonly PageBox PageBox;
        public readonly LyricWindow LyricWindow;
        public ConfigWindow ConfigWindow;
        public readonly OverallKeyHook OverallKeyHook;

        public bool ForceExit = false;
        private WindowState _lastState;
        private bool _miniMode = false;
        public bool FullMode => FullModeArea.Visibility == Visibility.Visible;

        //local player control
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _statusTask;
        private bool _scrollLock;
        private PlayerStatus _tmpStatus = PlayerStatus.Stopped;
        private double _videoOffset;

        public MainWindow()
        {
            InitializeComponent();
            PageBox = new PageBox(MainGrid, "_main");
            LyricWindow = new LyricWindow();
            LyricWindow.Show();
            OverallKeyHook = new OverallKeyHook(this);
            TryBindHotkeys();
            Unosquare.FFME.MediaElement.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //// todo: This should be kept since the application exit last time.
            //BtnRecent_Click(sender, e);
            UpdateCollections();
            LoadSurfaceSettings();
            RunSurfaceUpdate();
            if (App.Config.CurrentPath != null && App.Config.Play.Memory)
            {
                var entries = App.Beatmaps.GetBeatmapByIdentities(App.Config.CurrentList);
                App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, entries: entries);
                bool play = App.Config.Play.AutoPlay;
                PlayNewFile(App.Config.CurrentPath, play);
            }

            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source?.AddHook(HwndMessageHook);

            bool? sb = await App.Updater.CheckUpdateAsync();
            if (sb.HasValue && sb.Value)
            {
                NewVersionWindow newVersionWindow = new NewVersionWindow(App.Updater.NewRelease, this);
                newVersionWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Update collections in the navigation bar.
        /// </summary>
        public void UpdateCollections()
        {
            var list = (List<Collection>)DbOperator.GetCollections();
            list.Reverse();
            CollectionList.DataContext = list;
        }

        /// <summary>
        /// Clear things.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.Config.General.ExitWhenClosed == null && !ForceExit)
            {
                e.Cancel = true;
                FramePop.Navigate(new ClosingPage(this));
                return;
            }
            else if (App.Config.General.ExitWhenClosed == false && !ForceExit)
            {
                WindowState = WindowState.Minimized;
                Hide();
                e.Cancel = true;
                return;
            }

            ClearHitsoundPlayer();
            _cts.Dispose();
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
            LyricWindow.Dispose();
            NotifyIcon.Dispose();
            if (ConfigWindow == null || ConfigWindow.IsClosed) return;
            if (ConfigWindow.IsInitialized)
                ConfigWindow.Close();
        }

        /// <summary>
        /// Navigate search page.
        /// </summary>
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            MainFrame.Navigate(Pages.SearchPage);
            //MainFrame.Navigate(new Uri("Pages/SearchPage.xaml", UriKind.Relative), this);
        }

        /// <summary>
        /// Navigate find page.
        /// </summary>
        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            //MainFrame.Navigate(Pages.FindPage);
            PageBox.Show(Title, "功能完善中，敬请期待~", delegate { });
            //bool? b = await PageBox.ShowDialog(Title, "功能完善中，敬请期待~");
        }

        /// <summary>
        /// Navigate storyboard page.
        /// </summary>
        private void Storyboard_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
#if DEBUG
            MainFrame.Navigate(Pages.StoryboardPage);
#else
            PageBox.Show(Title, "功能完善中，敬请期待~", delegate { });
#endif
        }

        /// <summary>
        /// Navigate recent page.
        /// </summary>
        private void BtnRecent_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            MainFrame.Navigate(Pages.RecentPlayPage);
        }

        /// <summary>
        /// Navigate export page.
        /// </summary>
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            MainFrame.Navigate(Pages.ExportPage);
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(this));
        }

        /// <summary>
        /// Navigate collection page.
        /// </summary>
        private void CollectionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!ValidateDb()) return;
            if (CollectionList.SelectedItem == null)
                return;
            var collection = (Collection)CollectionList.SelectedItem;
            MainFrame.Navigate(new CollectionPage(this, DbOperator.GetCollectionById(collection.Id)));
        }

        /// <summary>
        /// Popup a dialog for settings.
        /// </summary>
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigWindow == null || ConfigWindow.IsClosed)
            {
                ConfigWindow = new ConfigWindow(this);
                ConfigWindow.Show();
            }
            else
            {
                if (ConfigWindow.IsInitialized)
                    ConfigWindow.Focus();
            }
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (FullModeArea.Visibility == Visibility.Hidden)
                FullModeArea.Visibility = Visibility.Visible;
            else if (FullModeArea.Visibility == Visibility.Visible)
                FullModeArea.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Play next song in playlist.
        /// </summary>
        public async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (App.HitsoundPlayer == null)
            {
                BtnOpen_Click(sender, e);
                return;
            }

            switch (App.HitsoundPlayer.PlayerStatus)
            {
                case PlayerStatus.Playing:
                    App.HitsoundPlayer.Pause();
                    if (VideoElement.Source != null) await VideoElement.Pause();
                    App.StoryboardProvider?.StoryboardTiming.Pause();
                    break;
                case PlayerStatus.Ready:
                case PlayerStatus.Stopped:
                case PlayerStatus.Paused:
                    App.HitsoundPlayer.Play();
                    if (VideoElement.Source != null) await VideoElement.Play();
                    App.StoryboardProvider?.StoryboardTiming.Start();
                    break;
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            PlayNewFile(LoadFile());
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            PlayNext(true, false);
        }

        public void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            PlayNext(true, true);
        }

        private void BtnMode_Click(object sender, RoutedEventArgs e)
        {
            PopMode.IsOpen = true;
        }

        /// <summary>
        /// Popup a dialog for adding music to a collection.
        /// </summary>
        private void BtnLike_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            var entry = App.Beatmaps.GetBeatmapByIdentity(App.PlayerList.CurrentIdentity);
            //var entry = App.PlayerList?.CurrentInfo.Entry;
            if (entry == null)
            {
                PageBox.Show(Title, "该图不存在于该osu!db中。", delegate { });
                return;
            }
            if (!_miniMode)
                FramePop.Navigate(new SelectCollectionPage(this, entry));
            else
            {
                var collection = DbOperator.GetCollections().First(k => k.Locked);
                if (App.PlayerList.CurrentInfo.IsFaved)
                {
                    DbOperator.RemoveMapFromCollection(entry, collection);
                    App.PlayerList.CurrentInfo.IsFaved = false;
                }
                else
                {
                    SelectCollectionPage.AddToCollection(collection, entry);
                    App.PlayerList.CurrentInfo.IsFaved = true;
                }
            }

            SetFaved(App.PlayerList.CurrentInfo.Identity);
        }

        private bool ValidateDb()
        {
            if (App.UseDbMode)
                return true;
            PageBox.Show(Title, "你尚未初始化osu!db，因此该功能不可用。", delegate { });
            return false;

        }

        private void BtnVolume_Click(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = true;
        }

        private void PlayMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (Single.IsChecked == true)
            {
                BtnMode.Content = "单曲播放";
                App.PlayerList.PlayerMode = PlayerMode.Single;
            }
            else if (SingleLoop.IsChecked == true)
            {
                BtnMode.Content = "单曲循环";
                App.PlayerList.PlayerMode = PlayerMode.SingleLoop;
            }
            else if (Normal.IsChecked == true)
            {
                BtnMode.Content = "顺序播放";
                App.PlayerList.PlayerMode = PlayerMode.Normal;
            }
            else if (Random.IsChecked == true)
            {
                BtnMode.Content = "随机播放";
                App.PlayerList.PlayerMode = PlayerMode.Random;
            }
            else if (Loop.IsChecked == true)
            {
                BtnMode.Content = "循环列表";
                App.PlayerList.PlayerMode = PlayerMode.Loop;
            }
            else if (LoopRandom.IsChecked == true)
            {
                BtnMode.Content = "随机循环";
                App.PlayerList.PlayerMode = PlayerMode.LoopRandom;
            }

            App.PlayerList.RefreshPlayList(PlayerList.FreshType.IndexOnly);
        }

        /// <summary>
        /// While popup lost focus, we should hide it.
        /// </summary>
        private void Popup_LostFocus(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = false;
            PopMode.IsOpen = false;
        }

        /// <summary>
        /// Play progress control.
        /// While drag started, slider's updating should be paused.
        /// </summary>
        private void PlayProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            _scrollLock = true;
        }

        /// <summary>
        /// Play progress control.
        /// While drag started, slider's updating should be recoverd.
        /// </summary>
        private void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (App.HitsoundPlayer != null)
            {
                switch (App.HitsoundPlayer.PlayerStatus)
                {
                    case PlayerStatus.Playing:
                        if (VideoElement.Source != null)
                            VideoElement.Position = new TimeSpan(0, 0, 0, 0, (int)(PlayProgress.Value + _videoOffset));
                        App.HitsoundPlayer.SetTime((int)PlayProgress.Value);
                        App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, true);
                        break;
                    case PlayerStatus.Paused:
                    case PlayerStatus.Stopped:
                        if (VideoElement.Source != null)
                            VideoElement.Position = new TimeSpan(0, 0, 0, 0, (int)(PlayProgress.Value + _videoOffset));
                        App.HitsoundPlayer.SetTime((int)PlayProgress.Value, false);
                        App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, false);
                        break;
                }
            }

            _scrollLock = false;
        }

        /// <summary>
        /// Master Volume Settings
        /// </summary>
        private void MasterVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Main = (float)(MasterVolume.Value / 100);
        }

        /// <summary>
        /// Music Volume Settings
        /// </summary>
        private void MusicVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Music = (float)(MusicVolume.Value / 100);
        }

        /// <summary>
        /// Effect Volume Settings
        /// </summary>
        private void HitsoundVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Hitsound = (float)(HitsoundVolume.Value / 100);
        }

        /// <summary>
        /// Offset Settings
        /// </summary>
        private void Offset_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (App.HitsoundPlayer == null) return;
            App.HitsoundPlayer.SingleOffset = (int)Offset.Value;
            DbOperator.UpdateMap(App.PlayerList.CurrentInfo.Identity, App.HitsoundPlayer.SingleOffset);
        }

        #region Player

        /// <summary>
        /// Call a file dialog to open custom file.
        /// </summary>
        public string LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = @"请选择一个.osu文件",
                Filter = @"Osu Files(*.osu)|*.osu"
            };
            var result = openFileDialog.ShowDialog();
            return (result.HasValue && result.Value) ? openFileDialog.FileName : null;
        }

        /// <summary>
        /// Play a new file by file path.
        /// </summary>
        private async void PlayNewFile(string path, bool play)
        {
            if (path == null) return;
            if (File.Exists(path))
            {
                try
                {
                    var osu = new OsuFile(path);
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot locate.", fi.FullName);
                    var dir = fi.Directory.FullName;

                    /* Clear */
                    ClearHitsoundPlayer();

                    /* Set new hitsound player*/
                    App.HitsoundPlayer = new HitsoundPlayer(path, osu);
                    _cts = new CancellationTokenSource();

                    /* Set Meta */
                    MapIdentity nowIdentity = new MapIdentity(fi.Directory.Name, App.HitsoundPlayer.Osufile.Metadata.Version); ;
                    MapInfo mapInfo = DbOperator.GetMapFromDb(nowIdentity);
                    BeatmapEntry entry = App.PlayerList.Entries.GetBeatmapByIdentity(nowIdentity);
                    OsuFile osuFile = App.HitsoundPlayer.Osufile;

                    LblTitle.Content = App.HitsoundPlayer.Osufile.Metadata.GetUnicodeTitle();
                    LblArtist.Content = App.HitsoundPlayer.Osufile.Metadata.GetUnicodeArtist();
                    ((ToolTip)NotifyIcon.TrayToolTip).Content = (string)LblArtist.Content + " - " + (string)LblTitle.Content;
                    bool isFaved = SetFaved(nowIdentity);
                    App.HitsoundPlayer.SingleOffset = mapInfo.Offset;
                    Offset.Value = App.HitsoundPlayer.SingleOffset;

                    App.PlayerList.CurrentInfo =
                        new CurrentInfo(osuFile.Metadata.Artist,
                            osuFile.Metadata.ArtistUnicode,
                            osuFile.Metadata.Title,
                            osuFile.Metadata.TitleUnicode,
                            osuFile.Metadata.Creator,
                            osuFile.Metadata.Source,
                            osuFile.Metadata.TagList,
                            osuFile.Metadata.BeatmapID,
                            osuFile.Metadata.BeatmapSetID,
                            entry?.DiffStarRatingStandard[Mods.None] ?? 0,
                            osuFile.Difficulty.HPDrainRate,
                            osuFile.Difficulty.CircleSize,
                            osuFile.Difficulty.ApproachRate,
                            osuFile.Difficulty.OverallDifficulty,
                            App.MusicPlayer?.Duration ?? 0,
                            nowIdentity,
                            mapInfo,
                            entry,
                            isFaved);

                    /* Set Lyric */
                    SetLyric();

                    /* Set Progress */
                    //PlayProgress.Value = App.HitsoundPlayer.SingleOffset;
                    PlayProgress.Maximum = App.HitsoundPlayer.Duration;
                    PlayProgress.Value = 0;
                    LblTotal.Content = new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.Duration).ToString(@"mm\:ss");
                    LblNow.Content = new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.PlayTime).ToString(@"mm\:ss");
#if DEBUG
                    /* Set Storyboard */
                    if (false) App.StoryboardProvider.LoadStoryboard(dir, App.HitsoundPlayer.Osufile);
#endif
                    if (VideoElement != null)
                    {
                        await VideoElement.Stop();
                        VideoElement.Position = new TimeSpan(0);
                    }
                    /* Set Video */
                    if (FullMode && !_miniMode)
                    {
                        var videoName = App.HitsoundPlayer.Osufile.Events.VideoInfo?.Filename;
                        if (videoName == null)
                        {
                            VideoElement.Source = null;
                            VideoElementBorder.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            var vPath = Path.Combine(dir, videoName);
                            if (File.Exists(vPath))
                            {
                                VideoElement.Source = new Uri(vPath);
                                _videoOffset = -App.HitsoundPlayer.Osufile.Events.VideoInfo.Offset;
                                VideoElement.Position = new TimeSpan(0, 0, 0, 0, (int)_videoOffset);
                            }
                            else
                            {
                                VideoElement.Source = null;
                                VideoElementBorder.Visibility = Visibility.Hidden;
                            }
                        }
                    }

                    /* Set Background */
                    if (App.HitsoundPlayer.Osufile.Events.BackgroundInfo != null)
                    {
                        var bgPath = Path.Combine(dir, App.HitsoundPlayer.Osufile.Events.BackgroundInfo.Filename);
                        BlurScene.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                        Thumb.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                    }
                    else
                        BlurScene.Source = null;

                    /* Start Play */
                    switch (MainFrame.Content)
                    {
                        case RecentPlayPage recentPlayPage:
                            var item = recentPlayPage.ViewModels.FirstOrDefault(k =>
                                k.GetIdentity().Equals(nowIdentity));
                            recentPlayPage.RecentList.SelectedItem = item;
                            break;
                        case CollectionPage collectionPage:
                            collectionPage.MapList.SelectedItem =
                                collectionPage.ViewModels.FirstOrDefault(k =>
                                    k.GetIdentity().Equals(nowIdentity));
                            break;
                    }

                    if (play)
                    {
                        if (FullMode && !_miniMode && VideoElement?.Source != null)
                        {
                            await VideoElement.Play();
                        }
                        App.HitsoundPlayer.Play();
                    }
                    App.Config.CurrentPath = path;
                    App.SaveConfig();

                    RunSurfaceUpdate();
                    DbOperator.UpdateMap(nowIdentity);
                }
                catch (MultiTimingSectionException ex)
                {
                    PageBox.Show(Title, @"铺面读取时发生问题：" + ex.Message, () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    });
                }
                catch (BadOsuFormatException ex)
                {
                    PageBox.Show(Title, @"铺面读取时发生问题：" + ex.Message, () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    });
                }
                catch (VersionNotSupportedException ex)
                {
                    PageBox.Show(Title, @"铺面读取时发生问题：" + ex.Message, () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    });
                }
                catch (Exception ex)
                {
                    PageBox.Show(Title, @"发生未处理的异常问题：" + (ex.InnerException ?? ex), () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    });
                    Console.WriteLine(ex);
                }
            }
            else
            {
                PageBox.Show(Title, string.Format(@"所选文件不存在{0}。",
                        App.Beatmaps == null ? "" : " ，可能是db没有及时更新。请关闭此播放器或osu后重试"),
                    () => { });
            }
        }

        private bool SetFaved(MapIdentity identity)
        {
            var map = DbOperator.GetMapFromDb(identity);
            var album = DbOperator.GetCollectionsByMap(map);
            bool faved = album != null && album.Any(k => k.Locked);
            BtnLike.Background = faved
                     ? (_miniMode ? (Brush)ToolControl.FindResource("FavedS") : (Brush)ToolControl.FindResource("Faved"))
                     : (_miniMode ? (Brush)ToolControl.FindResource("FavS") : (Brush)ToolControl.FindResource("Fav"));
            return faved;
        }

        public void PlayNewFile(string path)
        {
            PlayNewFile(path, true);
        }

        /// <summary>
        /// Play next song in list if list exist.
        /// </summary>
        /// <param name="isManual">Whether it is called by user (Click next button manually)
        /// or called by application (A song finshed).</param>
        /// <param name="isNext"></param>
        private void PlayNext(bool isManual, bool isNext)
        {
            if (App.HitsoundPlayer == null) return;
            var result = App.PlayerList.PlayTo(isNext, isManual, out var entry);
            switch (result)
            {
                case PlayerList.ChangeType.Keep:
                    App.HitsoundPlayer.Play();
                    break;

                case PlayerList.ChangeType.Stop:
                    App.HitsoundPlayer.Stop();
                    break;
                case PlayerList.ChangeType.Change:
                default:
                    var path = Path.Combine(new FileInfo(App.Config.General.DbPath).Directory.FullName, "Songs",
                        entry.FolderName, entry.BeatmapFileName);
                    PlayNewFile(path);
                    break;
            }
        }

        private void ClearHitsoundPlayer()
        {
            _cts.Cancel();
            Task.WaitAll(_statusTask);
            App.HitsoundPlayer?.Stop();
            App.HitsoundPlayer?.Dispose();
            App.HitsoundPlayer = null;
        }

        #endregion

        /// <summary>
        /// Call lyric provider to check lyric
        /// todo: this should run synchronously.
        /// </summary>
        public void SetLyric()
        {
            if (!LyricWindow.IsVisible) return;
            if (App.HitsoundPlayer == null) return;
            var lyric = App.LyricProvider.GetLyric(App.HitsoundPlayer.Osufile.Metadata.GetUnicodeArtist(),
                App.HitsoundPlayer.Osufile.Metadata.GetUnicodeTitle(), App.MusicPlayer.Duration);
            LyricWindow.SetNewLyric(lyric, App.HitsoundPlayer.Osufile);
            LyricWindow.StartWork();
        }

        #region Surface

        /// <summary>
        /// Initialize default player settings.
        /// </summary>
        private void LoadSurfaceSettings()
        {
            MasterVolume.Value = App.Config.Volume.Main * 100;
            MusicVolume.Value = App.Config.Volume.Music * 100;
            HitsoundVolume.Value = App.Config.Volume.Hitsound * 100;
        }

        /// <summary>
        /// Start a task to update player info (Starter).
        /// </summary>
        private void RunSurfaceUpdate()
        {
            _statusTask = Task.Run(new Action(UpdateSurface), _cts.Token);
        }

        /// <summary>
        /// Start a task to update player info (Looping in another thread).
        /// </summary>
        private void UpdateSurface()
        {
            const int interval = 50;
            while (!_cts.IsCancellationRequested)
            {
                if (App.HitsoundPlayer == null)
                {
                    Thread.Sleep(interval);
                    continue;
                }

                if (_tmpStatus != App.HitsoundPlayer.PlayerStatus)
                {
                    var s = App.HitsoundPlayer.PlayerStatus;
                    switch (s)
                    {
                        case PlayerStatus.Playing:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                BtnPlay.Style = (Style)FindResource("PauseButtonStyle");
                            }));
                            break;
                        case PlayerStatus.Finished:
                            Dispatcher.BeginInvoke(new Action(() => { PlayNext(false, true); }));
                            break;
                        case PlayerStatus.Stopped:
                        case PlayerStatus.Paused:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (App.HitsoundPlayer == null) return;
                                var playTime = Math.Min(App.HitsoundPlayer.PlayTime, PlayProgress.Maximum);
                                BtnPlay.Style = (Style)FindResource("PlayButtonStyle");
                                PlayProgress.Value = playTime < 0 ? 0 : playTime;
                                LblTotal.Content =
                                    new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.Duration).ToString(@"mm\:ss");
                                LblNow.Content =
                                    new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.PlayTime).ToString(@"mm\:ss");
                            }));
                            break;
                    }

                    _tmpStatus = App.HitsoundPlayer.PlayerStatus;
                }

                if (_tmpStatus == PlayerStatus.Playing && !_scrollLock)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        var playTime = Math.Min(App.HitsoundPlayer.PlayTime, PlayProgress.Maximum);
                        PlayProgress.Maximum = App.HitsoundPlayer.Duration;
                        PlayProgress.Value = playTime < 0
                            ? 0
                            : (playTime > PlayProgress.Maximum ? PlayProgress.Maximum : playTime);
                        LblTotal.Content = new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.Duration).ToString(@"mm\:ss");
                        LblNow.Content = new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.PlayTime).ToString(@"mm\:ss");
                    }));
                }

                Thread.Sleep(interval);
            }
        }

        #endregion

        private void TryBindHotkeys()
        {
            var page = new Pages.Settings.HotKeyPage(this);
            OverallKeyHook.AddKeyHook(page.PlayPause.Name, () => { BtnPlay_Click(null, null); });
            OverallKeyHook.AddKeyHook(page.Previous.Name, () =>
            {
                //TODO
            });
            OverallKeyHook.AddKeyHook(page.Next.Name, () => { BtnNext_Click(null, null); });
            OverallKeyHook.AddKeyHook(page.VolumeUp.Name, () => { App.Config.Volume.Main += 0.05f; });
            OverallKeyHook.AddKeyHook(page.VolumeDown.Name, () => { App.Config.Volume.Main -= 0.05f; });
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
                if (LyricWindow.IsHide)
                    LyricWindow.Show();
                else
                    LyricWindow.Hide();
            });
            GC.SuppressFinalize(page);
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                return;
            _lastState = WindowState;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private const int WmExitSizeMove = 0x232;
        private IntPtr HwndMessageHook(IntPtr wnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WmExitSizeMove:
                    if (Height <= MinHeight && !_miniMode)
                    {
                        ToMiniMode();
                    }
                    handled = true;
                    break;
            }
            return IntPtr.Zero;
        }

        private void BtnMax_Click(object sender, RoutedEventArgs e)
        {
            ToNormalMode();
        }

        private void ToMiniMode()
        {
            _miniMode = true;
            Topmost = true;
            MinHeight = 48 + 38 + 2;
            Height = MinHeight;
            MinWidth = 360 + 38;
            Width = MinWidth;
            BtnPrev.Margin = new Thickness(5);
            BtnPlay.Margin = new Thickness(5);
            BtnNext.Margin = new Thickness(5);
            BtnMode.Margin = new Thickness(2);
            BtnLike.Margin = new Thickness(2);
            BtnVolume.Margin = new Thickness(2);
            LblArtist.Margin = new Thickness(0, -2, 0, -2);
            LblTitle.Margin = new Thickness(0, -2, 0, -2);

            BtnLike.Width = BtnLike.Height = 18;
            BtnVolume.Width = BtnVolume.Height = 18;
            BtnMode.Width = BtnMode.Height = 18;

            LblMeta.Orientation = Orientation.Vertical;
            FuncPanel.Orientation = Orientation.Vertical;

            BtnMax.Visibility = Visibility.Visible;
            BorderMini.Visibility = Visibility.Visible;
            // modoules
            BtnOpen.Visibility = Visibility.Collapsed;
            Thumb.Visibility = Visibility.Collapsed;
            Grip.Visibility = Visibility.Collapsed;
            LblProgress.Visibility = Visibility.Collapsed;
            LblSeperate.Visibility = Visibility.Collapsed;
            PlayProgress.Visibility = Visibility.Collapsed;
            //area
            TitleBarArea.Visibility = Visibility.Collapsed;

            SetFaved(App.PlayerList.CurrentInfo.Identity);
        }

        private void ToNormalMode()
        {
            MinHeight = 100 + 38;
            Height = 720 + 38;
            MinWidth = 640 + 38;
            Width = 960 + 38;
            BtnPrev.Margin = new Thickness(8);
            BtnPlay.Margin = new Thickness(8);
            BtnNext.Margin = new Thickness(8);
            BtnMode.Margin = new Thickness(8);
            BtnLike.Margin = new Thickness(8);
            BtnVolume.Margin = new Thickness(8);
            LblArtist.Margin = new Thickness(0);
            LblTitle.Margin = new Thickness(0);

            BtnLike.Width = BtnLike.Height = 24;
            BtnVolume.Width = BtnVolume.Height = 24;
            BtnMode.Width = BtnMode.Height = 24;

            LblMeta.Orientation = Orientation.Horizontal;
            FuncPanel.Orientation = Orientation.Horizontal;

            BtnMax.Visibility = Visibility.Collapsed;
            BorderMini.Visibility = Visibility.Hidden;

            // modoules
            BtnOpen.Visibility = Visibility.Visible;
            Thumb.Visibility = Visibility.Visible;
            Grip.Visibility = Visibility.Visible;
            LblProgress.Visibility = Visibility.Visible;
            LblSeperate.Visibility = Visibility.Visible;
            PlayProgress.Visibility = Visibility.Visible;
            //area
            TitleBarArea.Visibility = Visibility.Visible;

            SetFaved(App.PlayerList.CurrentInfo.Identity);

            Topmost = false;
            _miniMode = false;
        }

        private async void VideoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Visible;
            await VideoElement.Play();
        }

        private void VideoElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Hidden;
            PageBox.Show("不支持的视频格式", e.ErrorException.ToString(), () => { });
        }

        private void BtnMini_Click(object sender, RoutedEventArgs e)
        {
            ToMiniMode();
        }
    }

    public class PageParts
    {
        public SearchPage SearchPage { get; set; }
        public StoryboardPage StoryboardPage { get; set; }
        public RecentPlayPage RecentPlayPage { get; set; }
        public FindPage FindPage { get; set; }
        public ExportPage ExportPage { get; set; }
    }
}
