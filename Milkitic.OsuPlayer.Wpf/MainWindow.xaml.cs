using Microsoft.Win32;
using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Wpf.Data;
using Milkitic.OsuPlayer.Wpf.Models;
using Milkitic.OsuPlayer.Wpf.Pages;
using Milkitic.OsuPlayer.Wpf.Utils;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Collection = Milkitic.OsuPlayer.Wpf.Data.Collection;

namespace Milkitic.OsuPlayer.Wpf
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public PageParts Pages => new PageParts
        {
            SearchPage = new SearchPage(this),
            RecentPlayPage = new RecentPlayPage(this),
            FindPage = new FindPage(this),
            StoryboardPage = new StoryboardPage(this),
        };

        public PageBox PageBox;
        private LyricWindow _lyricWindow;

        public MainWindow()
        {
            InitializeComponent();
            PageBox = new PageBox(MainGrid, "_main");
            _lyricWindow = new LyricWindow();
            _lyricWindow.Show();
        }

        public void AddToCollection(BeatmapEntry entry)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(Pages.RecentPlayPage);
            UpdateCollections();

            LoadSurfaceSettings();
            RunSurfaceUpdate();
        }

        public void UpdateCollections()
        {
            var list = (List<Collection>)DbOperator.GetCollections();
            list.Reverse();
            CollectionList.DataContext = list;
        }

        private void Window_Closing(object sender, EventArgs e)
        {
            ClearHitsoundPlayer();
            _cts.Dispose();
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
            _lyricWindow.Dispose();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            PlayNewFile(LoadFile());
        }

        /// <summary>
        /// Navigation Search
        /// </summary>
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(Pages.SearchPage);
            //MainFrame.Navigate(new Uri("Pages/SearchPage.xaml", UriKind.Relative), this);
        }

        /// <summary>
        /// Navigation Find
        /// </summary>
        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            //MainFrame.Navigate(Pages.FindPage);
            PageBox.Show(Title, "功能完善中，敬请期待~", delegate { });
            //bool? b = await PageBox.ShowDialog(Title, "功能完善中，敬请期待~");

        }

        /// <summary>
        /// Navigation Storyboard
        /// </summary>
        private void Storyboard_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            MainFrame.Navigate(Pages.StoryboardPage);
#else
            PageBox.Show(Title, "功能完善中，敬请期待~", delegate { });
#endif
        }

        /// <summary>
        /// Navigation Recent
        /// </summary>
        private void BtnRecent_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(Pages.RecentPlayPage);
        }

        /// <summary>
        /// Navigation Export
        /// </summary>
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(this));
        }

        private void CollectionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CollectionList.SelectedItem == null)
                return;
            var collection = (Collection)CollectionList.SelectedItem;
            MainFrame.Navigate(new CollectionPage(this, collection));
        }

        private void BtnFeedback_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {

        }


        private List<BeatmapEntry> _playList = new List<BeatmapEntry>();
        private List<int> _indexes = new List<int>();
        private int _point;

        //local control
        private PlayerMode PlayerMode { get; set; } = PlayerMode.LoopRandom;
        private PlayListMode PlayListMode { get; set; }

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _statusTask;
        private bool _scrollLock;
        private bool _isManualStop = true;
        private PlayerStatus _status = PlayerStatus.Stopped;
        private (string Version, string Folder) _nowInfo;

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (App.HitsoundPlayer == null)
            {
                PlayNewFile(LoadFile());
                return;
            }

            switch (App.HitsoundPlayer.PlayerStatus)
            {
                case PlayerStatus.Playing:
                    App.HitsoundPlayer.Pause();
                    App.StoryboardProvider?.StoryboardTiming.Pause();
                    break;
                case PlayerStatus.Stopped:
                case PlayerStatus.Paused:
                    App.HitsoundPlayer.Play();
                    App.StoryboardProvider?.StoryboardTiming.Start();
                    break;
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            AutoPlayNext(true);
        }

        private void BtnLike_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new SelectCollectionPage(this,
                App.Beatmaps.GetBeatmapsetsByFolder(_nowInfo.Folder)
                    .FirstOrDefault(k => k.Version == _nowInfo.Version)));
        }

        private void BtnVolume_Click(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = true;
        }

        private void Popup_LostFocus(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = false;
        }

        private void PlayProgress_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _scrollLock = true;
        }

        private void PlayProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (App.HitsoundPlayer != null)
            {
                switch (App.HitsoundPlayer.PlayerStatus)
                {
                    case PlayerStatus.Playing:
                        App.HitsoundPlayer.SetTime((int)PlayProgress.Value);
                        App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, true);
                        break;
                    case PlayerStatus.Paused:
                    case PlayerStatus.Stopped:
                        App.HitsoundPlayer.SetTime((int)PlayProgress.Value, false);
                        App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, false);
                        break;
                }
            }

            _scrollLock = false;
        }

        private void MasterVolume_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            App.Config.Volume.Main = (float)(MasterVolume.Value / 100);
        }

        private void MusicVolume_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            App.Config.Volume.Music = (float)(MusicVolume.Value / 100);
        }

        private void HitsoundVolume_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            App.Config.Volume.Hitsound = (float)(HitsoundVolume.Value / 100);
        }

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

        public void PlayNewFile(string path)
        {
            if (path == null) return;
            if (File.Exists(path))
            {
                try
                {
                    var osu = new OsuFile(path);
                    var fi = new FileInfo(path);
                    var dir = fi.Directory.FullName;
                    ClearHitsoundPlayer();
                    _isManualStop = true;
                    App.HitsoundPlayer = new HitsoundPlayer(path, osu);
                    _cts = new CancellationTokenSource();

                    /* Set Meta */
                    LblTitle.Content = App.HitsoundPlayer.Osufile.Metadata.GetUnicodeTitle();
                    LblArtist.Content = App.HitsoundPlayer.Osufile.Metadata.GetUnicodeArtist();

                    /* Set Lyric */
                    SetLyric();

                    /* Set Progress */
                    PlayProgress.Value = App.HitsoundPlayer.SingleOffset;

                    /* Set Storyboard */
#if DEBUG
                    if (false) App.StoryboardProvider.LoadStoryboard(dir, App.HitsoundPlayer.Osufile);
#endif
                    /* Set Background */
                    if (App.HitsoundPlayer.Osufile.Events.BackgroundInfo != null)
                    {
                        var bgPath = Path.Combine(dir, App.HitsoundPlayer.Osufile.Events.BackgroundInfo.Filename);
                        StoryboardScene.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                        Thumb.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                    }
                    else
                        StoryboardScene.Source = null;

                    /* Start Play */
                    App.HitsoundPlayer.Play();
                    RunSurfaceUpdate();

                    _nowInfo = (App.HitsoundPlayer.Osufile.Metadata.Version, fi.Directory.Name);
                    DbOperator.UpdateMap(_nowInfo.Version, _nowInfo.Folder);
                    Pages.RecentPlayPage.UpdateList();
                }
                catch (MultiTimingSectionException ex)
                {
                    PageBox.Show(Title, @"铺面读取时发生问题：" + ex.Message, () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) AutoPlayNext(false);
                    });
                }
                catch (BadOsuFormatException ex)
                {
                    PageBox.Show(Title, @"铺面读取时发生问题：" + ex.Message, () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) AutoPlayNext(false);
                    });
                }
                catch (VersionNotSupportedException ex)
                {
                    PageBox.Show(Title, @"铺面读取时发生问题：" + ex.Message, () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) AutoPlayNext(false);
                    });
                }
                catch (Exception ex)
                {
                    PageBox.Show(Title, @"发生未处理的异常问题：" + (ex.InnerException ?? ex), () =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) AutoPlayNext(false);
                    });
                }
            }
            else
            {
                PageBox.Show(Title, string.Format(@"所选文件不存在{0}。",
                        App.Beatmaps == null ? "" : " ，可能是db没有及时更新。请关闭此播放器或osu后重试"),
                    () => { });
            }
        }

        private void SetLyric()
        {
            if (!_lyricWindow.IsVisible) return;
            var lyric = App.LyricProvider.GetLyric(App.HitsoundPlayer.Osufile.Metadata.GetUnicodeArtist(),
                App.HitsoundPlayer.Osufile.Metadata.GetUnicodeTitle(), App.MusicPlayer.Duration);
            _lyricWindow.SetNewLyric(lyric, App.HitsoundPlayer.Osufile);
            _lyricWindow.StartWork();
        }

        private void AutoPlayNext(bool isManual)
        {
            if (App.Beatmaps == null) return;
            if (_point > _indexes.Count - 1)
            {
                if (_indexes.Count == 0) return;
                else if (PlayerMode == PlayerMode.Loop || PlayerMode == PlayerMode.LoopRandom)
                {
                    FillPlayList(false, true);
                }
                else
                {
                    if (isManual)
                    {
                        FillPlayList(false, true);
                    }
                    else
                    {
                        _isManualStop = true;
                        return;
                    }
                }
            }
            else if (_point == -1)
            {
                _point = _indexes.Count - 1;
            }
            BeatmapEntry map = _playList[_indexes[_point]];
            _point++;
            var path = Path.Combine(new FileInfo(App.Config.DbPath).Directory.FullName, "Songs", map.FolderName,
                map.BeatmapFileName);
            PlayNewFile(path);
        }

        public void FillPlayList(bool refreshList, bool refreshIndex, PlayListMode? playListMode = null)
        {
            if (playListMode != null) PlayListMode = playListMode.Value;
            if (refreshList || _playList.Count == 0)
                switch (PlayListMode)
                {
                    case PlayListMode.RecentList:
                        _playList = App.Beatmaps.GetRecentListFromDb().ToList();
                        break;
                    default:
                    case PlayListMode.Collection:
                        throw new NotImplementedException();
                        break;
                }

            if (refreshIndex || _indexes == null || _indexes.Count == 0)
                switch (PlayerMode)
                {
                    default:
                    case PlayerMode.Normal:
                    case PlayerMode.Loop:
                        _indexes = _playList.Select((o, i) => i).ToList();
                        break;
                    case PlayerMode.Random:
                    case PlayerMode.LoopRandom:
                        _indexes = _playList.Select((o, i) => i).ShuffleToList();
                        break;
                }
            _point = 0;
        }

        private void ClearHitsoundPlayer()
        {
            _cts.Cancel();
            Task.WaitAll(_statusTask);
            App.HitsoundPlayer?.Stop();
            App.HitsoundPlayer?.Dispose();
            App.HitsoundPlayer = null;
        }

        private void LoadSurfaceSettings()
        {
            MasterVolume.Value = App.Config.Volume.Main * 100;
            MusicVolume.Value = App.Config.Volume.Music * 100;
            HitsoundVolume.Value = App.Config.Volume.Hitsound * 100;
        }

        private void RunSurfaceUpdate()
        {
            _statusTask = Task.Run(new Action(UpdateSurface), _cts.Token);
        }

        private void UpdateSurface()
        {
            const int interval = 10;
            while (!_cts.IsCancellationRequested)
            {
                if (App.HitsoundPlayer == null)
                {
                    Thread.Sleep(interval);
                    continue;
                }

                if (_status != App.HitsoundPlayer.PlayerStatus)
                {
                    var s = App.HitsoundPlayer.PlayerStatus;
                    switch (s)
                    {
                        case PlayerStatus.Playing:
                            _isManualStop = false;
                            Dispatcher.BeginInvoke(new Action(() => { BtnPlay.Style = (Style)FindResource("PauseButtonStyle"); }));
                            break;
                        case PlayerStatus.Stopped when !_isManualStop:
                            Dispatcher.BeginInvoke(new Action(() => { AutoPlayNext(false); }));
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

                    _status = App.HitsoundPlayer.PlayerStatus;
                }

                if (_status == PlayerStatus.Playing && !_scrollLock)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (App.HitsoundPlayer == null) return;
                        var playTime = Math.Min(App.HitsoundPlayer.PlayTime, PlayProgress.Maximum);
                        PlayProgress.Maximum = App.HitsoundPlayer.Duration;
                        PlayProgress.Value = playTime < 0 ? 0 : (playTime > PlayProgress.Maximum ? PlayProgress.Maximum : playTime);
                        LblTotal.Content = new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.Duration).ToString(@"mm\:ss");
                        LblNow.Content = new TimeSpan(0, 0, 0, 0, App.HitsoundPlayer.PlayTime).ToString(@"mm\:ss");
                    }));
                }

                Thread.Sleep(interval);
            }
        }
    }

    public class PageParts
    {
        public SearchPage SearchPage { get; set; }
        public StoryboardPage StoryboardPage { get; set; }
        public RecentPlayPage RecentPlayPage { get; set; }
        public FindPage FindPage { get; set; }
    }
}
