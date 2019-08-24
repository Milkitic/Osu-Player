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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Milky.OsuPlayer.Control.Notification;
using Milky.OsuPlayer.Media.Audio.Music;
using Milky.WpfApi;
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
        private TimeSpan _initialVideoPosition;
        private bool _playAfterSeek;

        //private bool EnableVideo => PlayerViewModel.Current.EnableVideo;
        private bool IsVideoPlaying => VideoElement.Source != null;
        private void Controller_OnNewFileLoaded(object sender, HandledEventArgs e)
        {
            var osuFile = _playerInst.AudioPlayer.OsuFile;
            var path = _playList.CurrentInfo.Path;
            var dir = Path.GetDirectoryName(path);

            Execute.OnUiThread(() =>
            {
                Console.WriteLine("id:" + Thread.CurrentThread.ManagedThreadId);
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
                        SafelyRecreateVideoElement(ViewModel.Player.EnableVideo).Wait();

                        if (PlayerViewModel.Current.EnableVideo)
                        {
                            var videoName = osuFile.Events.VideoInfo?.Filename;
                            if (videoName == null)
                            {
                                VideoElement.Source = null;
                                //VideoElementBorder.Visibility = Visibility.Hidden;
                            }
                            else
                            {
                                var vPath = Path.Combine(dir, videoName);
                                if (File.Exists(vPath))
                                {
                                    _playAfterSeek = true;
                                    VideoElement.Source = new Uri(vPath);

                                    _videoOffset = -(osuFile.Events.VideoInfo.Offset);
                                    if (_videoOffset >= 0)
                                    {
                                        _waitAction = () => { };
                                        _initialVideoPosition = TimeSpan.FromMilliseconds(_videoOffset);
                                    }
                                    else
                                    {
                                        _waitAction = () => { Thread.Sleep(TimeSpan.FromMilliseconds(-_videoOffset)); };
                                    }
                                }
                                else
                                {
                                    VideoElement.Source = null;
                                    //VideoElementBorder.Visibility = Visibility.Hidden;
                                }
                            }
                        }
                    }

                    /* Set Background */
                    if (osuFile.Events.BackgroundInfo != null)
                    {
                        var bgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                        BackImage.Source = File.Exists(bgPath) ? new BitmapImage(new Uri(bgPath)) : null;
                    }
                    else
                    {
                        BackImage.Source = null;
                    }

                    if (ViewModel.Player.EnableVideo && VideoElement?.Source != null)
                    {
                        BackImage.Opacity = 0.15;
                        BlendBorder.Visibility = Visibility.Visible;
                        e.Handled = true;
                    }
                    else
                    {
                        BackImage.Opacity = 1;
                        BlendBorder.Visibility = Visibility.Collapsed;
                    }

                }
                catch (Exception ex)
                {
                    Notification.Show(@"发生未处理的错误：" + (ex.InnerException ?? ex));
                }
            });
        }

        private void Controller_OnPlayClick()
        {
            if (IsVideoPlaying)
            {
                VideoElement.Play();
            }
        }

        private void Controller_OnPauseClick()
        {
            if (IsVideoPlaying)
            {
                VideoElement.Pause();
            }
        }

        private async void Controller_OnProgressDragComplete(object sender, DragCompleteEventArgs e)
        {
            var isVideoPlaying = IsVideoPlaying;
            if (isVideoPlaying)
            {
                e.Handled = true;

                Services.Get<PlayersInst>().AudioPlayer.Pause();
                var milliseconds = e.CurrentPlayTime;
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

                switch (e.PlayerStatus)
                {
                    case PlayerStatus.Playing:
                        _playAfterSeek = true;
                        break;
                    case PlayerStatus.Paused:
                    case PlayerStatus.Stopped:
                        _playAfterSeek = false;
                        break;
                }
            }
        }

        private void Controller_OnThumbClick(object sender, RoutedEventArgs e)
        {
            MainFrame.Content = null;
            //if (!PlayerViewModel.Current.EnableVideo)
            //    PlayerViewModel.Current.EnableVideo = true;
            //else if (PlayerViewModel.Current.EnableVideo)
            //{
            //    if (ResizableArea.Margin == new Thickness(5))
            //        SetFullScr();
            //    else
            //        SetFullScrMini();
            //}
        }

        private async void Controller_OnLikeClick(object sender, RoutedEventArgs e)
        {
            var entry = _beatmapDbOperator.GetBeatmapByIdentifiable(Services.Get<PlayerList>().CurrentIdentity);
            //var entry = App.PlayerList?.CurrentInfo.Beatmap;
            if (entry == null)
            {
                Notification.Show("该图不存在于该osu!db中", Title);
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
            if (Execute.CheckDispatcherAccess())
            {
                VideoElement.Stop();
                BindVideoElement();
            }
            else
            {
                await VideoElement.Stop();
                Execute.OnUiThread(BindVideoElement);
            }

            async void OnMediaOpened(object sender, MediaOpenedEventArgs e)
            {
                VideoElementBorder.Visibility = Visibility.Visible;
                if (!PlayerViewModel.Current.EnableVideo)
                    return;
                await Task.Run(() => _waitAction?.Invoke());

                if (VideoElement == null/* || VideoElement.IsDisposed*/)
                    return;
                await VideoElement.Play();
                VideoElement.Position = _initialVideoPosition;
            }

            async void OnMediaFailed(object sender, MediaFailedEventArgs e)
            {
                VideoElementBorder.Visibility = Visibility.Hidden;
                //MsgBox.Show(this, e.ErrorException.ToString(), "不支持的视频格式", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!PlayerViewModel.Current.EnableVideo)
                    return;
                await SafelyRecreateVideoElement(false);
                PlayController.TogglePlay();
            }

            void OnMediaEnded(object sender, EventArgs e)
            {
                if (VideoElement == null /*|| VideoElement.IsDisposed*/)
                    return;
                //VideoElement.Position = TimeSpan.Zero;
            }

            void OnSeekingStarted(object sender, EventArgs e)
            { }

            void OnSeekingEnded(object sender, EventArgs e)
            {
                if (!PlayerViewModel.Current.EnableVideo)
                    return;
                Services.Get<PlayersInst>().AudioPlayer.SetTime((int)(VideoElement.Position.TotalMilliseconds - _videoOffset), false);
                if (_playAfterSeek)
                {
                    Services.Get<PlayersInst>().AudioPlayer.Play();
                    VideoElement.Play();
                }
                else
                {
                    Services.Get<PlayersInst>().AudioPlayer.Pause();
                    VideoElement.Pause();
                }
            }

            void BindVideoElement()
            {
                VideoElement.Position = TimeSpan.Zero;
                VideoElement.Source = null;

                VideoElement.MediaOpened -= OnMediaOpened;
                VideoElement.MediaFailed -= OnMediaFailed;
                VideoElement.MediaEnded -= OnMediaEnded;
                VideoElement.SeekingStarted -= OnSeekingStarted;
                VideoElement.SeekingEnded -= OnSeekingEnded;
                Services.Get<PlayersInst>().AudioPlayer.PlayerStarted -= OnAudioPlayerOnPlayerStarted;
                Services.Get<PlayersInst>().AudioPlayer.PlayerPaused -= OnAudioPlayerOnPlayerPaused;
                VideoElement.Dispose();
                VideoElement = null;
                VideoElementBorder.Child = null;
                //VideoElementBorder.Visibility = Visibility.Hidden;
                VideoElement = new Unosquare.FFME.MediaElement { IsMuted = true, LoadedBehavior = MediaPlaybackState.Manual, Visibility = Visibility.Visible, };
                VideoElement.MediaOpened += OnMediaOpened;
                VideoElement.MediaFailed += OnMediaFailed;
                VideoElement.MediaEnded += OnMediaEnded;

                if (showVideo)
                {
                    VideoElement.SeekingStarted += OnSeekingStarted;
                    VideoElement.SeekingEnded += OnSeekingEnded;

                    Services.Get<PlayersInst>().AudioPlayer.PlayerStarted += OnAudioPlayerOnPlayerStarted;
                    Services.Get<PlayersInst>().AudioPlayer.PlayerPaused += OnAudioPlayerOnPlayerPaused;
                }

                VideoElementBorder.Child = VideoElement;
            }

            void OnAudioPlayerOnPlayerPaused(object sender, ProgressEventArgs e)
            {
                //VideoElement.Pause();
            }

            void OnAudioPlayerOnPlayerStarted(object sender, ProgressEventArgs e)
            {
                //VideoElement.Play();
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
