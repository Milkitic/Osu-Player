using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Anotar.NLog;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Audio.Mixing;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.NotificationComponent;
using Milki.OsuPlayer.Wpf;
using Unosquare.FFME.Common;

namespace Milki.OsuPlayer.UserControls;

public class AnimationControlVm : VmBase
{
}

/// <summary>
/// AnimationControl.xaml 的交互逻辑
/// </summary>
public partial class AnimationControl : UserControl
{
    private readonly PlayerService _playerService;
    private readonly AnimationControlVm _viewModel;

    private Task _waitTask;
    private TimeSpan _initialVideoPosition;
    private double _videoOffset;

    private PlayerService.PlayItemLoadContext _loadContext;

    public AnimationControl()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playerService = App.Current.ServiceProvider.GetService<PlayerService>()!;

            _playerService.LoadStarted += PlayerService_LoadStarted;
            _playerService.LoadBackgroundInfoFinished += PlayerService_LoadBackgroundInfoFinished;
            _playerService.LoadVideoRequested += PlayerService_LoadVideoRequested;
            _playerService.PlayerStarted += PlayerService_PlayerStarted;
            _playerService.PlayerPaused += PlayerService_PlayerPaused;
            _playerService.PlayerStopped += PlayerService_PlayerStopped;
            _playerService.PlayerSeek += PlayerService_PlayerSeek;
            SharedVm.Default.PropertyChanged += Shared_PropertyChanged;
        }

        DataContext = _viewModel = new AnimationControlVm();
        InitializeComponent();
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            VideoElement.IsMuted = true;
        }

        var path = Path.Combine(AppSettings.Directories.ResourceDir, "official", "registration.jpg");
        if (File.Exists(path))
        {
            BackImage.Source = new BitmapImage(new Uri(path));
            BackImage.Opacity = 1;
        }
    }

    private async void Shared_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SharedVm.EnableVideo):
                if (_loadContext == null) return;

                if (SharedVm.Default.EnableVideo)
                    await InitVideoAsync(_loadContext);
                else
                    await CloseVideoAsync();

                break;
        }
    }

    private async ValueTask PlayerService_LoadStarted(PlayerService.PlayItemLoadContext loadContext)
    {
        _loadContext = loadContext;
        await CloseVideoAsync();

        Execute.OnUiThread(() =>
        {
            BackImage.Opacity = 1;
            BlendBorder.Visibility = Visibility.Collapsed;
        });

        //AppSettings.Default.PlaySection.PropertyChanged -= Play_PropertyChanged;
    }

    private ValueTask PlayerService_LoadBackgroundInfoFinished(PlayerService.PlayItemLoadContext loadContext)
    {
        if (loadContext.BackgroundPath == null)
        {
            BackImage.Source = null;
        }
        else
        {
            BackImage.Source = new BitmapImage(new Uri(loadContext.BackgroundPath));
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask PlayerService_LoadVideoRequested(PlayerService.PlayItemLoadContext loadContext)
    {
        if (VideoElement == null) return;

        if (!SharedVm.Default.EnableVideo) return;

        await InitVideoAsync(loadContext);
    }

    private async ValueTask PlayerService_PlayerStarted(Audio.OsuMixPlayer arg)
    {
        await PlayVideo();
    }

    private async ValueTask PlayerService_PlayerPaused(Audio.OsuMixPlayer arg)
    {
        await PauseVideo();
    }

    private async ValueTask PlayerService_PlayerStopped(Audio.OsuMixPlayer arg)
    {
        await StopVideo();
    }

    private async ValueTask PlayerService_PlayerSeek(Audio.OsuMixPlayer player, TimeSpan timestamp)
    {
        var status = player.PlayerStatus;

        await player.Pause();
        var trueOffset = timestamp.TotalMilliseconds + _videoOffset;

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
        await player.Seek(VideoElement.Position - TimeSpan.FromMilliseconds(_videoOffset));

        LogTo.Warn("SetTime Done");

        async void OnVideoElementOnSeekingEnded(object sender, EventArgs e)
        {
            waitForSeek = false;
            if (status == PlayerStatus.Playing)
            {
                await player.Play();
                await PlayVideo();
            }
            else
            {
                await player.Pause();
                await PauseVideo();
            }
        }
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
        if (e.PropertyName == nameof(AppSettings.PlaySection.PlaybackRate))
            VideoElement.SpeedRatio = AppSettings.Default.PlaySection.PlaybackRate;
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
        if (@this.Effect is BlurEffect effect) effect.Radius = 30;
        else @this.Effect = new BlurEffect { Radius = 30 };
    }

    private async Task CloseVideoAsync()
    {
        await VideoElement.Stop();
        await VideoElement.Close();

        Execute.OnUiThread(() =>
        {
            VideoElementBorder.Visibility = Visibility.Hidden;
            VideoElement.SpeedRatio = AppSettings.Default.PlaySection.PlaybackRate;
        });
    }
    private async Task InitVideoAsync(PlayerService.PlayItemLoadContext loadContext)
    {
        if (loadContext.OsuFile?.Events?.VideoInfo == null || loadContext.VideoPath == null) return;

        _videoOffset = -(loadContext.OsuFile.Events.VideoInfo.Offset);
        if (_videoOffset >= 0)
        {
            _waitTask = Task.Delay(0);
            _initialVideoPosition = TimeSpan.FromMilliseconds(_videoOffset);
        }
        else
        {
            _waitTask = Task.Delay(TimeSpan.FromMilliseconds(-_videoOffset));
        }

        await VideoElement.Open(new Uri(loadContext.VideoPath));
        Execute.OnUiThread(() =>
        {
            BackImage.Opacity = 0.15;
            BlendBorder.Visibility = Visibility.Visible;
        });
    }

    private async void OnMediaOpened(object sender, MediaOpenedEventArgs e)
    {
        VideoElementBorder.Visibility = Visibility.Visible;
        if (!SharedVm.Default.EnableVideo) return;
        if (VideoElement == null) return;
        if (_loadContext?.PlayInstant == true)
        {
            await _waitTask;
            await VideoElement.Play();
            VideoElement.Position = _initialVideoPosition;
        }
    }

    private async void OnMediaFailed(object sender, MediaFailedEventArgs e)
    {
        VideoElementBorder.Visibility = Visibility.Hidden;
        LogTo.ErrorException("Error while loading video", e.ErrorException);
        Notification.Push("不支持的视频格式");
        if (!SharedVm.Default.EnableVideo) return;
        await CloseVideoAsync();
        await _playerService.TogglePlayAsync();
    }
}