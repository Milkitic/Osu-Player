using Microsoft.Win32;
using Milkitic.OsuLib;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Milkitic.OsuPlayer
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
            var ext = new FileInfo(openFileDialog.FileName).Extension;
            return (result.HasValue && result.Value) ? openFileDialog.FileName : null;
        }

        public void PlayNewFile(string path)
        {
            PlayNewFile(path, true);
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
                            entry != null
                                ? (entry.DiffStarRatingStandard.ContainsKey(Mods.None)
                                    ? entry.DiffStarRatingStandard[Mods.None]
                                    : 0)
                                : 0,
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
                    /* Set Video */
                    if (VideoElement != null)
                    {
                        await ClearVideoElement();

                        if (FullMode && !_miniMode)
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
                                    VideoElement.Position = new TimeSpan(0, 0, 0, 0, (int)_videoOffset);
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

        private async Task ClearVideoElement()
        {
            await VideoElement.Stop();
            VideoElement.Position = new TimeSpan(0);
            VideoElement.Source = null;
            VideoElement.Dispose();
            VideoElement = null;
            VideoElementBorder.Visibility = Visibility.Hidden;
            VideoElement = new Unosquare.FFME.MediaElement
            {
                IsMuted = true,
                LoadedBehavior = MediaState.Manual,
                Visibility = System.Windows.Visibility.Visible
            };
            VideoElement.MediaOpened += VideoElement_MediaOpened;
            VideoElement.MediaFailed += VideoElement_MediaFailed;
            VideoElementBorder.Children.Add(VideoElement);
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
    }
}
