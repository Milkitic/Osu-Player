using Microsoft.Win32;
using OSharp.Beatmap;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Media;
using Milky.OsuPlayer.Media.Music;
using Milky.OsuPlayer.Pages;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
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

        public async Task PlayNewFile(string path)
        {
            await PlayNewFile(path, true);
        }

        private StoryboardWindow _sbWindow;
        private Action _waitAction;
        private MyCancellationTokenSource _waitActionCts;
        private TimeSpan _position;
        private bool _forcePaused;
        private bool _videoPlay;

        /// <summary>
        /// Play a new file by file path.
        /// </summary>
        private async Task PlayNewFile(string path, bool play)
        {
            if (path == null) return;
            if (File.Exists(path))
            {
                try
                {
                    var osu = OsuFile.ReadFromFile(path);
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
                    MapIdentity nowIdentity =
                        new MapIdentity(fi.Directory.Name, App.HitsoundPlayer.Osufile.Metadata.Version);

                    MapInfo mapInfo = DbOperator.GetMapFromDb(nowIdentity);
                    //BeatmapEntry entry = App.PlayerList.Entries.GetBeatmapByIdentity(nowIdentity);
                    BeatmapEntry entry = App.Beatmaps.GetBeatmapByIdentity(nowIdentity);
                    OsuFile osuFile = App.HitsoundPlayer.Osufile;

                    LblTitle.Content = App.HitsoundPlayer.Osufile.Metadata.TitleMeta.Unicode;
                    LblArtist.Content = App.HitsoundPlayer.Osufile.Metadata.ArtistMeta.Unicode;
                    ((ToolTip)NotifyIcon.TrayToolTip).Content =
                        (string)LblArtist.Content + " - " + (string)LblTitle.Content;
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
                            osuFile.Metadata.BeatmapId,
                            osuFile.Metadata.BeatmapSetId,
                            entry != null
                                ? (entry.DiffStarRatingStandard.ContainsKey(Mods.None)
                                    ? entry.DiffStarRatingStandard[Mods.None]
                                    : 0)
                                : 0,
                            osuFile.Difficulty.HpDrainRate,
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
                    if (false)
                    {
                        if (_sbWindow == null || _sbWindow.IsClosed)
                        {
                            _sbWindow = new StoryboardWindow();
                            _sbWindow.Show();
                            App.StoryboardProvider = new Media.Storyboard.StoryboardProvider(_sbWindow);
                        }

                        App.StoryboardProvider.LoadStoryboard(dir, App.HitsoundPlayer.Osufile);
                    }
#endif
                    /* Set Video */
                    bool showVideo = ViewModel.EnableVideo && !ViewModel.IsMiniMode;
                    if (VideoElement != null)
                    {
                        await ClearVideoElement(showVideo);

                        if (showVideo)
                        {
                            var videoName = App.HitsoundPlayer.Osufile.Events.VideoInfo?.Filename;
                            if (videoName == null)
                            {
                                VideoElement.Source = null;
                                VideoElementBorder.Visibility = System.Windows.Visibility.Hidden;
                            }
                            else
                            {
                                var vPath = Path.Combine(dir, videoName);
                                if (File.Exists(vPath))
                                {
                                    VideoElement.Source = new Uri(vPath);
                                    _videoOffset = -App.HitsoundPlayer.Osufile.Events.VideoInfo.Offset;
                                    if (_videoOffset >= 0)
                                    {
                                        _waitAction = () => { };
                                        _position = TimeSpan.FromMilliseconds(_videoOffset);
                                    }
                                    else
                                    {
                                        _waitAction = () => { Thread.Sleep(TimeSpan.FromMilliseconds(-_videoOffset)); };
                                    }
                                }
                                else
                                {
                                    VideoElement.Source = null;
                                    VideoElementBorder.Visibility = System.Windows.Visibility.Hidden;
                                }
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

                    _videoPlay = play;
                    if (play)
                    {
                        if (showVideo && VideoElement?.Source != null)
                        {

                            VideoPlay();
                            //App.HitsoundPlayer.Play();
                        }
                        else
                            App.HitsoundPlayer.Play();
                    }

                    //if (!App.PlayerList.Entries.Any(k => k.GetIdentity().Equals(nowIdentity)))
                    //    App.PlayerList.Entries.Add(entry);
                    App.Config.CurrentPath = path;
                    App.SaveConfig();

                    RunSurfaceUpdate();
                    DbOperator.UpdateMap(nowIdentity);
                }
                catch (RepeatTimingSectionException ex)
                {
                    var result = MsgBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Title, MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    }
                }
                catch (BadOsuFormatException ex)
                {
                    var result = MsgBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Title, MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    }
                }
                catch (VersionNotSupportedException ex)
                {
                    var result = MsgBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Title, MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    }
                }
                catch (Exception ex)
                {
                    var result = MsgBox.Show(this, @"发生未处理的异常问题：" + (ex.InnerException ?? ex), Title,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    if (result == MessageBoxResult.OK)
                    {
                        if (App.HitsoundPlayer == null) return;
                        if (App.HitsoundPlayer.PlayerStatus != PlayerStatus.Playing) PlayNext(false, true);
                    }

                    Console.WriteLine(ex);
                }
            }
            else
            {
                MsgBox.Show(this, string.Format(@"所选文件不存在{0}。", App.Beatmaps == null
                        ? ""
                        : " ，可能是db没有及时更新。请关闭此播放器或osu后重试"),
                    Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void VideoPlay()
        {
            //await VideoElement.Pause();

            //await VideoElement.Play();
        }

        private async Task VideoJumpTo(int milliseconds)
        {
            _waitActionCts = new MyCancellationTokenSource();
            Guid? guid = _waitActionCts?.Guid;
            var trueOffset = milliseconds + _videoOffset;
            if (trueOffset < 0)
            {
                await VideoElement.Pause();
                VideoElement.Position = TimeSpan.FromMilliseconds(0);

                await Task.Run(() => { Thread.Sleep(TimeSpan.FromMilliseconds(-trueOffset)); });
                if (_waitActionCts?.Guid != guid || _waitActionCts?.IsCancellationRequested == true)
                    return;
                if (!_forcePaused)
                    await VideoElement.Play();
                else
                    await VideoElement.Pause();
            }
            else
            {
                //if (_mediaEnded)
                //{
                //    VideoElement.Position = TimeSpan.FromMilliseconds(0);
                //    await Task.Run(() => { Thread.Sleep(10); });
                //}
                if (!_forcePaused)
                    await VideoElement.Play();
                else
                    await VideoElement.Pause();
                VideoElement.Position = TimeSpan.FromMilliseconds(trueOffset);
            }
        }

        private async Task ClearVideoElement(bool seek)
        {
            await Task.Run(async () =>
            {
                await VideoElement.Stop();
                Dispatcher.Invoke(() =>
                {
                    VideoElement.Position = new TimeSpan(0);
                    VideoElement.Source = null;
                    VideoElement.Dispose();
                    VideoElement = null;
                    VideoElementBorder.Visibility = Visibility.Hidden;
                    VideoElement = new Unosquare.FFME.MediaElement
                    {
                        IsMuted = true,
                        LoadedBehavior = MediaState.Manual,
                        Visibility = System.Windows.Visibility.Visible,
                    };
                    VideoElement.MediaOpened += VideoElement_MediaOpened;
                    VideoElement.MediaFailed += VideoElement_MediaFailed;
                    VideoElement.MediaEnded += (sender, e) => { VideoElement.Position = TimeSpan.FromSeconds(0); };
                    if (seek)
                    {
                        VideoElement.SeekingStarted += (sender, e) => { };
                        VideoElement.SeekingEnded += (sender, e) =>
                        {
                            if (!_videoPlay) return;
                            PlayMedia();
                        };
                    }
                    VideoElementBorder.Children.Add(VideoElement);
                });
            });
        }

        private void PlayMedia()
        {
            if (_forcePaused) return;
            App.HitsoundPlayer.Play();
            App.StoryboardProvider?.StoryboardTiming.Start();
        }

        private void PauseMedia()
        {
            App.HitsoundPlayer.Pause();
            App.StoryboardProvider?.StoryboardTiming.Pause();
        }

        /// <summary>
        /// Play next song in list if list exist.
        /// </summary>
        /// <param name="isManual">Whether it is called by user (Click next button manually)
        /// or called by application (A song finshed).</param>
        /// <param name="isNext"></param>
        private async void PlayNext(bool isManual, bool isNext)
        {
            if (App.HitsoundPlayer == null) return;
            var result = App.PlayerList.PlayTo(isNext, isManual, out var entry);
            switch (result)
            {
                //case PlayerList.ChangeType.Keep:
                //    await VideoJumpTo(0);
                //    App.StoryboardProvider?.StoryboardTiming.SetTiming(0, false);
                //    PlayMedia();
                //    break;

                case PlayerList.ChangeType.Stop:
                    //var path2 = Path.Combine(new FileInfo(App.Config.General.DbPath).Directory.FullName, "Songs",
                    //    entry.FolderName, entry.BeatmapFileName);
                    //PlayNewFile(path2, false);
                    _videoPlay = false;
                    _forcePaused = true;
                    App.HitsoundPlayer.Stop();
                    break;
                case PlayerList.ChangeType.Change:
                default:
                    var path = Path.Combine(new FileInfo(App.Config.General.DbPath).Directory.FullName, "Songs",
                        entry.FolderName, entry.BeatmapFileName);
                    await PlayNewFile(path);
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
    }

    internal class MyCancellationTokenSource : CancellationTokenSource
    {
        public Guid Guid { get; }

        public MyCancellationTokenSource()
        {
            Guid = Guid.NewGuid();
        }
    }
}
