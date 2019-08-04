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
using Milky.OsuPlayer.Control.Notification;
using Unosquare.FFME.Common;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
        private PlayersInst _playerInst = Services.Get<PlayersInst>();
        private OsuDbInst _dbInst = Services.Get<OsuDbInst>();
        private PlayerList _playList = Services.Get<PlayerList>();

        private Action _waitAction;
        private MyCancellationTokenSource _waitActionCts;
        private TimeSpan _position;

        private bool _videoPlay;

        private bool PlayVideo => PlayerViewModel.Current.EnableVideo && !ViewModel.IsMiniMode;
        private async void Controller_OnNewFileLoaded(object sender, RoutedEventArgs e)
        {
            var osuFile = _playerInst.AudioPlayer.OsuFile;
            var path = _playList.CurrentInfo.Path;
            var dir = Path.GetDirectoryName(path);
            try
            {
                /* Set Lyric */
                SetLyricSynchronously();

                /* Set Storyboard */
                if (true)
                {
                    // Todo: Set Storyboard
                }

                /* Set Video */
                if (VideoElement != null)
                {
                    await SafelyRecreateVideoElement(PlayVideo);

                    if (PlayVideo)
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
                }
                else
                {
                    BlurScene.Source = null;
                }

                if (PlayVideo && VideoElement?.Source != null)
                {
                    // use event to control here.
                    //VideoPlay();
                }

            }
            catch (Exception ex)
            {
                App.NotificationList.Add(new NotificationOption
                {
                    Content = @"发生未处理的错误：" + (ex.InnerException ?? ex)
                });
            }
        }

        private async void Controller_OnProgressDragComplete(object sender, RoutedEventArgs e)
        {
            if (PlayVideo)
            {
                e.Handled = true;

                if (e is DragCompleteRoutedEventArgs e1)
                {
                    var milliseconds = e1.CurrentPlayTime;
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
                    }


                    if (trueOffset >= 0)
                    {
                        VideoElement.Position = TimeSpan.FromMilliseconds(trueOffset);
                    }

                    switch (e1.PlayerStatus)
                    {
                        case PlayerStatus.Playing:
                            await VideoElement.Play();
                            break;
                        case PlayerStatus.Paused:
                        case PlayerStatus.Stopped:
                            await VideoElement.Pause();
                            break;
                    }
                }
            }
        }

        private void Controller_OnThumbClick(object sender, RoutedEventArgs e)
        {
            if (!PlayerViewModel.Current.EnableVideo)
                PlayerViewModel.Current.EnableVideo = true;
            else if (PlayerViewModel.Current.EnableVideo)
            {
                if (ResizableArea.Margin == new Thickness(5))
                    SetFullScr();
                else
                    SetFullScrMini();
            }
        }

        private async void Controller_OnLikeClick(object sender, RoutedEventArgs e)
        {
            var entry = _beatmapDbOperator.GetBeatmapByIdentifiable(Services.Get<PlayerList>().CurrentIdentity);
            //var entry = App.PlayerList?.CurrentInfo.Beatmap;
            if (entry == null)
            {
                App.NotificationList.Add(new NotificationOption
                {
                    Title = Title,
                    Content = "该图不存在于该osu!db中。"
                });
                return;
            }

            if (!ViewModel.IsMiniMode)
                FramePop.Navigate(new SelectCollectionPage(entry));
            else
            {
                var collection = _appDbOperator.GetCollections().First(k => k.Locked);
                if (Services.Get<PlayerList>().CurrentInfo.IsFavorite)
                {
                    _appDbOperator.RemoveMapFromCollection(entry, collection);
                    Services.Get<PlayerList>().CurrentInfo.IsFavorite = false;
                }
                else
                {
                    await SelectCollectionPage.AddToCollectionAsync(collection, entry);
                    Services.Get<PlayerList>().CurrentInfo.IsFavorite = true;
                }
            }

            IsMapFavorite(Services.Get<PlayerList>().CurrentInfo.Identity);
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
                PlayController.TogglePlay();
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
                PlayController.TogglePlay();
            }
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
