using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Unosquare.FFME.Common;

namespace Milky.OsuPlayer.UserControls
{
    public class AnimationControlVm : VmBase
    {
        public SharedVm Player { get; } = SharedVm.Default;
    }

    /// <summary>
    /// AnimationControl.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationControl : UserControl
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Task _waitTask;
        private TimeSpan _initialVideoPosition;

        private double _videoOffset;

        private AnimationControlVm _viewModel;

        public AnimationControl()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _viewModel = (AnimationControlVm)DataContext;

            var path = Path.Combine(Domain.ResourcePath, "default.jpg");
            if (File.Exists(path))
            {
                BackImage.Source = new BitmapImage(new Uri(path));
                BackImage.Opacity = 1;
            }

            if (_controller == null) return;
            _controller.LoadStarted += Controller_LoadStarted;
            _controller.BackgroundInfoLoaded += Controller_BackgroundInfoLoaded;
            _controller.VideoLoadRequested += Controller_VideoLoadRequested;
        }

        private async void Controller_LoadStarted(BeatmapContext arg1, CancellationToken arg2)
        {
            await SafelyRecreateVideoElement(_viewModel.Player.EnableVideo);

            Execute.OnUiThread(() =>
            {
                BackImage.Opacity = 1;
                BlendBorder.Visibility = Visibility.Collapsed;
            });

            AppSettings.Default.Play.PropertyChanged -= Play_PropertyChanged;
        }

        private void Controller_BackgroundInfoLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
        {
            BackImage.Source = beatmapCtx.BeatmapDetail.BackgroundPath == null
                ? null
                : new BitmapImage(new Uri(beatmapCtx.BeatmapDetail.BackgroundPath));
        }

        private async void Controller_VideoLoadRequested(BeatmapContext beatmapCtx, CancellationToken ct)
        {
            if (VideoElement == null) return;

            if (!SharedVm.Default.EnableVideo) return;

            _videoOffset = -(beatmapCtx.OsuFile.Events.VideoInfo.Offset);
            if (_videoOffset >= 0)
            {
                _waitTask = Task.Delay(0);
                _initialVideoPosition = TimeSpan.FromMilliseconds(_videoOffset);
            }
            else
            {
                _waitTask = Task.Delay(TimeSpan.FromMilliseconds(-_videoOffset));
            }

            await VideoElement.Open(new Uri(beatmapCtx.BeatmapDetail.VideoPath));
            Execute.OnUiThread(() =>
            {
                BackImage.Opacity = 0.15;
                BlendBorder.Visibility = Visibility.Visible;
            });

            beatmapCtx.PlayHandle = async () =>
            {
                Logger.Warn("Called PlayHandle()");
                await _controller.Player.Play();
                await PlayVideo();
            };

            beatmapCtx.PauseHandle = async () =>
            {
                Logger.Warn("Called PauseHandle()");
                await _controller.Player.Pause();
                await PauseVideo();
            };

            beatmapCtx.StopHandle = async () =>
            {
                Logger.Warn("Called StopHandle()");
                await _controller.Player.Stop();
                await StopVideo();
            };

            beatmapCtx.SetTimeHandle = async (time, play) =>
            {
                Logger.Warn("Called PlayHandle()");
                await _controller.Player.Pause();
                var trueOffset = time + _videoOffset;

                bool waitForSeek = true;

                VideoElement.SeekingEnded += OnVideoElementOnSeekingEnded;
                if (trueOffset < 0)
                {
                    Execute.OnUiThread(async () =>
                    {
                        await PauseVideo();
                        VideoElement.Position = TimeSpan.FromMilliseconds(0);
                    });

                    await Task.Run(() => { Thread.Sleep(TimeSpan.FromMilliseconds(-trueOffset)); });
                }
                else if (trueOffset >= 0)
                {
                    Execute.OnUiThread(() => VideoElement.Position = TimeSpan.FromMilliseconds(trueOffset));
                }

                await Task.Run(() =>
                {
                    while (waitForSeek)
                    {
                        Thread.Sleep(1);
                    }
                });

                VideoElement.SeekingEnded -= OnVideoElementOnSeekingEnded;
                await _controller.Player.SkipTo(VideoElement.Position - TimeSpan.FromMilliseconds(_videoOffset));

                Logger.Warn("SetTime Done");
                async void OnVideoElementOnSeekingEnded(object sender, EventArgs e)
                {
                    waitForSeek = false;
                    if (play)
                    {
                        await _controller.Player.Play();
                        await PlayVideo();
                    }
                    else
                    {
                        await _controller.Player.Pause();
                        await PauseVideo();
                    }
                }
            };

            AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
        }

        private async Task PlayVideo()
        {
            if (VideoElement.MediaState == MediaPlaybackState.Play)
                return;
            await VideoElement.Play();
        }

        private async Task PauseVideo()
        {
            if (VideoElement.MediaState != MediaPlaybackState.Play)
                return;
            await VideoElement.Pause();
        }

        private async Task StopVideo()
        {
            if (VideoElement.MediaState != MediaPlaybackState.Play)
                return;
            await VideoElement.Stop();
        }

        private void Play_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.Play.PlaybackRate))
                VideoElement.SpeedRatio = AppSettings.Default.Play.PlaybackRate;
        }

        public bool IsBlur
        {
            get => (bool)GetValue(IsBlurProperty);
            set => SetValue(IsBlurProperty, value);
        }

        public static readonly DependencyProperty IsBlurProperty =
            DependencyProperty.Register("IsBlur",
                typeof(bool),
                typeof(AnimationControl),
                new PropertyMetadata(false, OnBlurChanged));

        private static void OnBlurChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is AnimationControl @this && e.NewValue is bool useEffect)) return;
            var effect = @this.Effect as BlurEffect;
            if ((AppSettings.Default.Interface.MinimalMode || !useEffect))
            {
                if (effect != null) effect.Radius = 0;
                @this.Effect = null;
            }
            else
            {
                if (effect != null) effect.Radius = 30;
                else @this.Effect = new BlurEffect { Radius = 30 };
            }
        }

        private async Task SafelyRecreateVideoElement(bool showVideo)
        {
            await VideoElement.Stop();
            await VideoElement.Close();

            Execute.OnUiThread(() =>
            {
                //VideoElement.MediaOpened -= OnMediaOpened;
                //VideoElement.MediaFailed -= OnMediaFailed;
                VideoElementBorder.Visibility = Visibility.Hidden;
                VideoElement.SpeedRatio = AppSettings.Default.Play.PlaybackRate;
                //VideoElement.MediaOpened += OnMediaOpened;
                //VideoElement.MediaFailed += OnMediaFailed;
            });
        }

        private async void OnMediaOpened(object sender, MediaOpenedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Visible;
            if (!SharedVm.Default.EnableVideo) return;
            if (VideoElement == null) return;
            if (_controller.PlayList.CurrentInfo.PlayInstantly)
            {
                await _waitTask;
                await VideoElement.Play();
                VideoElement.Position = _initialVideoPosition;
            }
        }

        private async void OnMediaFailed(object sender, MediaFailedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Hidden;
            Logger.Error(e.ErrorException, "Error while loading video");
            Notification.Push("不支持的视频格式");
            if (!SharedVm.Default.EnableVideo) return;
            await SafelyRecreateVideoElement(false);
            await _controller.Player.TogglePlay();
        }
    }
}
