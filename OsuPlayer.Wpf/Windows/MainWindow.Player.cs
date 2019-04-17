using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.ViewModels;
using OSharp.Beatmap;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Unosquare.FFME.Common;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
        /// <summary>
        /// Call a file dialog to open custom file.
        /// </summary>
        private static string LoadFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = @"请选择一个.osu文件",
                Filter = @"Osu Files(*.osu)|*.osu"
            };
            var result = openFileDialog.ShowDialog();
            return (result.HasValue && result.Value) ? openFileDialog.FileName : null;
        }

        public async Task PlayNewFile(Beatmap map)
        {
            string path = map.InOwnFolder
              ? Path.Combine(Domain.CustomSongPath, map.FolderName, map.BeatmapFileName)
              : Path.Combine(Domain.OsuSongPath, map.FolderName, map.BeatmapFileName);
            await PlayNewFile(path);
        }

        public async Task PlayNewFile(string path)
        {
            await PlayNewFile(path, true);
        }

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
            var sw = Stopwatch.StartNew();

            var playerInst = InstanceManage.GetInstance<PlayersInst>();
            var dbInst = InstanceManage.GetInstance<OsuDbInst>();
            ComponentPlayer audioPlayer = null;

            if (path == null)
                return;
            if (File.Exists(path))
            {
                //try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(path); //50 ms
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                        throw new FileNotFoundException("Cannot locate.", fi.FullName);
                    var dir = fi.Directory.FullName;

                    /* Clear */
                    ClearHitsoundPlayer();

                    /* Set new hitsound player*/
                    playerInst.SetAudioPlayer(path, osuFile);
                    audioPlayer = playerInst.AudioPlayer;
                    SignUpPlayerEvent(audioPlayer);
                    await audioPlayer.InitializeAsync(); //700 ms

                    /* Set Meta */
                    var nowIdentity = new MapIdentity(fi.Directory.Name, osuFile.Metadata.Version);

                    MapInfo mapInfo = DbOperate.GetMapFromDb(nowIdentity);
                    Beatmap beatmap = BeatmapQuery.FilterByIdentity(nowIdentity);

                    bool isFavorite = IsMapFavorite(mapInfo); //50 ms

                    audioPlayer.HitsoundOffset = mapInfo.Offset;
                    Offset.Value = audioPlayer.HitsoundOffset;

                    var currentInfo = new CurrentInfo(
                        osuFile.Metadata.Artist,
                        osuFile.Metadata.ArtistUnicode,
                        osuFile.Metadata.Title,
                        osuFile.Metadata.TitleUnicode,
                        osuFile.Metadata.Creator,
                        osuFile.Metadata.Source,
                        osuFile.Metadata.TagList,
                        osuFile.Metadata.BeatmapId,
                        osuFile.Metadata.BeatmapSetId,
                        beatmap?.DiffSrNoneStandard ?? 0,
                        osuFile.Difficulty.HpDrainRate,
                        osuFile.Difficulty.CircleSize,
                        osuFile.Difficulty.ApproachRate,
                        osuFile.Difficulty.OverallDifficulty,
                        audioPlayer.Duration,
                        nowIdentity,
                        mapInfo,
                        beatmap,
                        isFavorite); // 20 ms
                    InstanceManage.GetInstance<PlayerList>().CurrentInfo = currentInfo;
                    ViewModel.Player.CurrentInfo = currentInfo;

                    /*start of ui*/
                    LblTitle.Content = osuFile.Metadata.TitleMeta.ToUnicodeString();
                    LblArtist.Content = osuFile.Metadata.ArtistMeta.ToUnicodeString();
                    ((ToolTip)NotifyIcon.TrayToolTip).Content =
                        (string)LblArtist.Content + " - " + (string)LblTitle.Content;
                    /*end of ui*/

                    /* Set Lyric */
                    SetLyricSynchronously();

                    /* Set Progress */
                    PlayProgress.Maximum = audioPlayer.Duration;
                    PlayProgress.Value = 0;

                    PlayerViewModel.Current.Duration = InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Duration;

                    /* Set Storyboard */
                    if (true)
                    {
                        // Todo: Set Storyboard
                    }

                    /* Set Video */
                    bool showVideo = PlayerViewModel.Current.EnableVideo && !ViewModel.IsMiniMode;
                    if (VideoElement != null)
                    {
                        await SafelyRecreateVideoElement(showVideo);

                        if (showVideo)
                        {
                            var videoName = osuFile.Events.VideoInfo?.Filename;
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
                                    _videoOffset = -(osuFile.Events.VideoInfo.Offset);
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
                                    VideoElementBorder.Visibility = Visibility.Hidden;
                                }
                            }
                        }
                    }

                    /* Set Background */
                    if (osuFile.Events.BackgroundInfo != null)
                    {
                        var bgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                        BlurScene.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                        Thumb.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                    }
                    else
                        BlurScene.Source = null;

                    /* Start Play */
                    switch (MainFrame.Content)
                    {
                        case RecentPlayPage recentPlayPage:
                            var item = recentPlayPage.DataModels.FirstOrDefault(k =>
                                k.GetIdentity().Equals(nowIdentity));
                            recentPlayPage.RecentList.SelectedItem = item;
                            break;
                        case CollectionPage collectionPage:
                            collectionPage.MapList.SelectedItem =
                                collectionPage.ViewModel.Beatmaps.FirstOrDefault(k =>
                                    k.GetIdentity().Equals(nowIdentity));
                            break;
                    }

                    _videoPlay = play;
                    if (play)
                    {
                        if (showVideo && VideoElement?.Source != null)
                        {
                            // use event to control here.
                            //VideoPlay();
                            //App.HitsoundPlayer.Play();
                        }
                        else
                            audioPlayer.Play();
                    }

                    //if (!App.PlayerList.Entries.Any(k => k.GetIdentity().Equals(nowIdentity)))
                    //    App.PlayerList.Entries.Add(entry);
                    PlayerConfig.Current.CurrentPath = path;
                    PlayerConfig.SaveCurrent();

                    DbOperate.UpdateMap(nowIdentity);
                }
                //catch (RepeatTimingSectionException ex)
                //{
                //    var result = MsgBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Title, MessageBoxButton.OK,
                //        MessageBoxImage.Warning);
                //    if (result == MessageBoxResult.OK)
                //    {
                //        if (audioPlayer == null) return;
                //        if (audioPlayer.PlayerStatus != PlayerStatus.Playing) await PlayNextAsync(false, true);
                //    }
                //}
                //catch (BadOsuFormatException ex)
                //{
                //    var result = MsgBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Title, MessageBoxButton.OK,
                //        MessageBoxImage.Warning);
                //    if (result == MessageBoxResult.OK)
                //    {
                //        if (audioPlayer == null) return;
                //        if (audioPlayer.PlayerStatus != PlayerStatus.Playing) await PlayNextAsync(false, true);
                //    }
                //}
                //catch (VersionNotSupportedException ex)
                //{
                //    var result = MsgBox.Show(this, @"铺面读取时发生问题：" + ex.Message, Title, MessageBoxButton.OK,
                //        MessageBoxImage.Warning);
                //    if (result == MessageBoxResult.OK)
                //    {
                //        if (audioPlayer == null) return;
                //        if (audioPlayer.PlayerStatus != PlayerStatus.Playing) await PlayNextAsync(false, true);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    var result = MsgBox.Show(this, @"发生未处理的异常问题：" + (ex.InnerException ?? ex), Title,
                //        MessageBoxButton.OK, MessageBoxImage.Error);
                //    if (result == MessageBoxResult.OK)
                //    {
                //        if (audioPlayer == null) return;
                //        if (audioPlayer.PlayerStatus != PlayerStatus.Playing) await PlayNextAsync(false, true);
                //    }

                //    Console.WriteLine(ex);
                //}
            }
            else
            {
                MsgBox.Show(this, @"所选文件不存在，可能是db没有及时更新。请关闭此播放器或osu后重试。",
                    Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        private void SignUpPlayerEvent(ComponentPlayer audioPlayer)
        {
            audioPlayer.PlayerLoaded += (sender, e) =>
            {
                var player = (ComponentPlayer)sender;
                Console.WriteLine(player.OsuFile.ToString() + @" PlayerLoaded.");
            };
            audioPlayer.PlayerFinished += async (sender, e) =>
            {
                await PlayNextAsync(false, true);
            };
            audioPlayer.PlayerPaused += (sender, e) =>
            {
                PlayerViewModel.Current.IsPlaying = false;
                PlayerViewModel.Current.Position = e.Position;
            };
            audioPlayer.PositionSet += (sender, e) =>
            {

            };
            audioPlayer.PlayerStarted += (sender, e) =>
            {
                PlayerViewModel.Current.IsPlaying = true;
                PlayerViewModel.Current.Position = e.Position;
            };
            audioPlayer.PlayerStopped += (sender, e) =>
            {

            };
            audioPlayer.PositionChanged += (sender, e) =>
            {
                if (!_scrollLock)
                {
                    PlayerViewModel.Current.Position = e.Position;
                    PlayProgress.Value = e.Position;
                }
            };
        }

        //private async void VideoPlay()
        //{
        //    //await VideoElement.Pause();

        //    //await VideoElement.Play();
        //}

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

        private async Task SafelyRecreateVideoElement(bool showVideo)
        {
            await Task.Run(async () =>
            {
                await VideoElement.Stop();
                Dispatcher.Invoke(() =>
                {
                    VideoElement.Position = TimeSpan.Zero;
                    VideoElement.Source = null;

                    VideoElement.MediaOpened -= OnMediaOpened;
                    VideoElement.MediaFailed -= OnMediaFailed;
                    VideoElement.MediaEnded -= OnMediaEnded;
                    VideoElement.SeekingStarted -= OnSeekingStarted;
                    VideoElement.SeekingEnded -= OnSeekingEnded;

                    //VideoElement.Dispose();
                    VideoElement = null;
                    VideoElementBorder.Visibility = Visibility.Hidden;
                    VideoElement = new Unosquare.FFME.MediaElement
                    {
                        IsMuted = true,
                        LoadedBehavior = MediaPlaybackState.Manual,
                        Visibility = Visibility.Visible,
                    };
                    VideoElement.MediaOpened += OnMediaOpened;
                    VideoElement.MediaFailed += OnMediaFailed;
                    VideoElement.MediaEnded += OnMediaEnded;

                    if (showVideo)
                    {
                        VideoElement.SeekingStarted += OnSeekingStarted;
                        VideoElement.SeekingEnded += OnSeekingEnded;
                    }

                    VideoElementBorder.Children.Add(VideoElement);
                });
            });

            async void OnMediaOpened(object sender, MediaOpenedEventArgs e)
            {
                VideoElementBorder.Visibility = Visibility.Visible;
                if (!_videoPlay)
                    return;
                await Task.Run(() => _waitAction?.Invoke());

                if (VideoElement == null/* || VideoElement.IsDisposed*/)
                    return;
                await VideoElement.Play();
                VideoElement.Position = _position;
            }

            async void OnMediaFailed(object sender, MediaFailedEventArgs e)
            {
                VideoElementBorder.Visibility = Visibility.Hidden;
                //MsgBox.Show(this, e.ErrorException.ToString(), "不支持的视频格式", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!_videoPlay)
                    return;
                await SafelyRecreateVideoElement(false);
                PlayMedia();
            }

            void OnMediaEnded(object sender, EventArgs e)
            {
                if (VideoElement == null /*|| VideoElement.IsDisposed*/)
                    return;
                VideoElement.Position = TimeSpan.Zero;
            }

            void OnSeekingStarted(object sender, EventArgs e)
            { }

            void OnSeekingEnded(object sender, EventArgs e)
            {
                if (!_videoPlay)
                    return;
                PlayMedia();
            }
        }

        private void PlayMedia()
        {
            if (_forcePaused)
                return;
            InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Play();
            // Todo: Set Storyboard
        }

        private void PauseMedia()
        {
            InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Pause();
            // Todo: Set Storyboard
        }

        /// <summary>
        /// Play next song in list if list exist.
        /// </summary>
        /// <param name="isManual">Whether it is called by user (Click next button manually)
        /// or called by application (A song finished).</param>
        /// <param name="isNext"></param>
        private async Task PlayNextAsync(bool isManual, bool isNext)
        {
            if (InstanceManage.GetInstance<PlayersInst>().AudioPlayer == null)
                return;
            (PlayerList.ChangeType result, Beatmap map) = await InstanceManage.GetInstance<PlayerList>().PlayToAsync(isNext, isManual);
            switch (result)
            {
                //case PlayerList.ChangeType.Keep:
                //    await VideoJumpTo(0);
                //    App.StoryboardProvider?.StoryboardTiming.SetTiming(0, false);
                //    PlayMedia();
                //    break;

                case PlayerList.ChangeType.Stop:
                    PlayerViewModel.Current.IsPlaying = false;
                    PlayerViewModel.Current.Position = 0;
                    _videoPlay = false;
                    _forcePaused = true;
                    break;
                case PlayerList.ChangeType.Change:
                default:
                    //var path = Path.Combine(new FileInfo(PlayerConfig.Current.General.DbPath).Directory.FullName, "Songs",
                    //    entry.FolderName, entry.BeatmapFileName);
                    //await PlayNewFile(path);
                    await PlayNewFile(map);
                    break;
            }
        }

        private void ClearHitsoundPlayer()
        {
            InstanceManage.GetInstance<PlayersInst>()?.ClearAudioPlayer();
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
