using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Milki.Extensions.MixPlayer;
using Milki.OsuPlayer.Audio.Playlist;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Common.Configuration;
using Milki.OsuPlayer.Presentation.Interaction;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Unosquare.FFME.Common;

namespace Milki.OsuPlayer.UserControls
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
        private BeatmapContext _beatmapCtx;

        public AnimationControl()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _viewModel = (AnimationControlVm)DataContext;

            var path = Path.Combine(Domain.ResourcePath, "official", "registration.jpg");
            if (File.Exists(path))
            {
                BackImage.Source = new BitmapImage(new Uri(path));
                BackImage.Opacity = 1;
            }

            if (_controller == null) return;
            _controller.LoadStarted += Controller_LoadStarted;
            _controller.BackgroundInfoLoaded += Controller_BackgroundInfoLoaded;
            _controller.VideoLoadRequested += Controller_VideoLoadRequested;
            _controller.InterfaceClearRequest += Controller_InterfaceClearRequest;
            _controller.LoadError += Controller_LoadError;

            SharedVm.Default.PropertyChanged += Shared_PropertyChanged;
            AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
        }

        private void Controller_LoadError(BeatmapContext arg1, Exception arg2)
        {
            _beatmapCtx = null;
        }

        private void Controller_InterfaceClearRequest()
        {
            _beatmapCtx = null;
        }

        private async void Shared_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SharedVm.EnableVideo):
                    if (_beatmapCtx == null) return;

                    if (SharedVm.Default.EnableVideo)
                        await InitVideoAsync(_beatmapCtx);
                    else
                        await CloseVideoAsync();

                    break;
            }
        }

        private async void Controller_LoadStarted(BeatmapContext beatmapCtx, CancellationToken ct)
        {
            _beatmapCtx = beatmapCtx;
            await CloseVideoAsync();

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

            await InitVideoAsync(beatmapCtx);
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

        private async Task CloseVideoAsync()
        {
            await VideoElement.Stop();
            await VideoElement.Close();

            if (_beatmapCtx != null)
            {
                _beatmapCtx.PlayHandle = async () => await _controller.Player.Play().ConfigureAwait(false);
                _beatmapCtx.PauseHandle = async () => await _controller.Player.Pause().ConfigureAwait(false);
                _beatmapCtx.StopHandle = async () => await _controller.Player.Stop().ConfigureAwait(false);
                _beatmapCtx.RestartHandle = async () =>
                {
                    await _beatmapCtx.StopHandle().ConfigureAwait(false);
                    await _beatmapCtx.PlayHandle().ConfigureAwait(false);
                };
                _beatmapCtx.TogglePlayHandle = async () =>
                {
                    if (_controller.Player.PlayStatus == PlayStatus.Ready ||
                        _controller.Player.PlayStatus == PlayStatus.Finished ||
                        _controller.Player.PlayStatus == PlayStatus.Paused)
                    {
                        await _beatmapCtx.PlayHandle().ConfigureAwait(false);
                    }
                    else if (_controller.Player.PlayStatus == PlayStatus.Playing)
                    {
                        await _beatmapCtx.PauseHandle().ConfigureAwait(false);
                    }
                };

                _beatmapCtx.SetTimeHandle = async (time, play) =>
                    await _controller.Player.SkipTo(TimeSpan.FromMilliseconds(time)).ConfigureAwait(false);
            }

            Execute.OnUiThread(() =>
            {
                VideoElementBorder.Visibility = Visibility.Hidden;
                VideoElement.SpeedRatio = AppSettings.Default.Play.PlaybackRate;
            });
        }
        private async Task InitVideoAsync(BeatmapContext beatmapCtx)
        {
            if (beatmapCtx.OsuFile.Events.VideoInfo == null) return;

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
            await CloseVideoAsync();
            await _controller.Player.TogglePlay();
        }
    }
}
