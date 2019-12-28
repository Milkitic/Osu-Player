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
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.Media.Audio.Core;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
using Unosquare.FFME.Common;
using ViewModelBase = Milky.WpfApi.ViewModelBase;

namespace Milky.OsuPlayer.Control
{
    public class AnimationControlVm : ViewModelBase
    {
        private PlayerViewModel _player;

        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// AnimationControl.xaml 的交互逻辑
    /// </summary>
    public partial class AnimationControl : UserControl
    {
        private PlayersInst _playerInst = Services.Get<PlayersInst>();
        private OsuDbInst _dbInst = Services.Get<OsuDbInst>();
        private PlayerList _playList = Services.Get<PlayerList>();

        private bool _playAfterSeek;
        private Action _waitAction;
        private TimeSpan _initialVideoPosition;
        private MyCancellationTokenSource _waitActionCts;

        private double _videoOffset;

        public AnimationControl()
        {
            InitializeComponent();
            var path = Path.Combine(Domain.ResourcePath, "default.jpg");
            if (File.Exists(path))
            {
                BackImage.Source = new BitmapImage(new Uri(path));
                BackImage.Opacity = 1;
            }

            ViewModel = (AnimationControlVm)DataContext;
            ViewModel.Player = PlayerViewModel.Current;
        }

        private void AnimationControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlayController.Default.OnNewFileLoaded += Controller_OnNewFileLoaded;
            PlayController.Default.OnPlayClick += Controller_OnPlayClick;
            PlayController.Default.OnPauseClick += Controller_OnPauseClick;
            PlayController.Default.OnProgressDragComplete += Controller_OnProgressDragComplete;
            AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
        }

        private void Play_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.Play.PlaybackRate):
                    if (!(VideoElement is null))
                        VideoElement.SpeedRatio = AppSettings.Default.Play.PlaybackRate;
                    break;
            }
        }

        public AnimationControlVm ViewModel { get; set; }

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

        private bool _pauseThisSession;

        private bool IsVideoPlaying => VideoElement.Source != null;

        private static void OnBlurChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is AnimationControl @this && e.NewValue is bool useEffect)) return;
            if (AppSettings.Default.Interface.MinimalMode)
            {
                if (@this.Effect is BlurEffect effect)
                {
                    effect.Radius = 0;
                }

                @this.Effect = null;
                return;
            }

            if (useEffect)
            {
                if (@this.Effect is BlurEffect effect)
                {
                    effect.Radius = 30;
                }
                else
                {
                    @this.Effect = new BlurEffect
                    {
                        Radius = 30
                    };
                }
            }
            else
            {
                if (@this.Effect is BlurEffect effect)
                {
                    effect.Radius = 0;
                }

                @this.Effect = null;
            }
        }
        private void Controller_OnNewFileLoaded(object sender, HandledEventArgs e)
        {
            var osuFile = _playerInst.AudioPlayer.OsuFile;
            var path = _playList.CurrentInfo.Path;
            var dir = Path.GetDirectoryName(path);
            _pauseThisSession = (bool)sender;
            Execute.OnUiThread(() =>
            {
                Console.WriteLine("id:" + Thread.CurrentThread.ManagedThreadId);
                try
                {

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

                    var defaultPath = Path.Combine(Domain.ResourcePath, "default.jpg");
                    /* Set Background */
                    if (osuFile.Events.BackgroundInfo != null)
                    {
                        var bgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                        BackImage.Source = File.Exists(bgPath)
                            ? new BitmapImage(new Uri(bgPath))
                            : (File.Exists(defaultPath)
                                ? new BitmapImage(new Uri(defaultPath))
                                : null);
                    }
                    else
                    {
                        BackImage.Source = null;
                        BackImage.Source = File.Exists(defaultPath)
                            ? new BitmapImage(new Uri(defaultPath))
                            : null;
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
                    OsuPlayer.Notification.Show(@"发生未处理的错误：" + (ex.InnerException ?? ex));
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
                if (_pauseThisSession)
                {
                    await VideoElement.Play();
                    VideoElement.Position = _initialVideoPosition;
                }
            }

            async void OnMediaFailed(object sender, MediaFailedEventArgs e)
            {
                VideoElementBorder.Visibility = Visibility.Hidden;
                //MsgBox.Show(this, e.ErrorException.ToString(), "不支持的视频格式", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!PlayerViewModel.Current.EnableVideo)
                    return;
                await SafelyRecreateVideoElement(false);
                PlayController.Default.TogglePlay();
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
                VideoElement = new Unosquare.FFME.MediaElement
                {
                    IsMuted = true,
                    LoadedBehavior = MediaPlaybackState.Manual,
                    Visibility = Visibility.Visible,
                    SpeedRatio = AppSettings.Default.Play.PlaybackRate
                };
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

        public void StartScene(Action action)
        {
            action?.Invoke();
        }
    }
}
