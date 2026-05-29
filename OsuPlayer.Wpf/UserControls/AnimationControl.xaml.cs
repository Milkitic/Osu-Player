using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Milki.Extensions.MixPlayer;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Unosquare.FFME.Common;

namespace Milky.OsuPlayer.UserControls;

public partial class AnimationControlVm : ObservableObject
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
        _controller.PlayStatusChanged += Controller_PlayStatusChanged;
        _controller.PositionSetRequested += Controller_PositionSetRequested;

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

        VideoElementBorder.Visibility = Visibility.Hidden;
        VideoElement.SpeedRatio = AppSettings.Default.Play.PlaybackRate;
    }

    private async Task InitVideoAsync(BeatmapContext beatmapCtx)
    {
        if (beatmapCtx.OsuFile.Events.VideoInfo == null) return;

        _videoOffset = -(beatmapCtx.OsuFile.Events.VideoInfo.Offset);
        await VideoElement.Open(new Uri(beatmapCtx.BeatmapDetail.VideoPath));
        BackImage.Opacity = 0.15;
        BlendBorder.Visibility = Visibility.Visible;

        AppSettings.Default.Play.PropertyChanged -= Play_PropertyChanged;
        AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
    }

    private async void Controller_PlayStatusChanged(PlayStatus status)
    {
        if (!ShouldSyncVideo(_beatmapCtx)) return;

        switch (status)
        {
            case PlayStatus.Playing:
                await PlayVideoFromCurrentPositionAsync();
                break;
            case PlayStatus.Paused:
                if (_controller.Player?.Position <= TimeSpan.FromMilliseconds(10))
                    await StopVideo();
                else
                    await PauseVideo();
                break;
            case PlayStatus.Finished:
                await StopVideo();
                break;
        }
    }

    private Task Controller_PositionSetRequested(BeatmapContext beatmapCtx, double time, bool play)
    {
        if (!ShouldSyncVideo(beatmapCtx)) return Task.CompletedTask;
        _ = Execute.OnUiThreadAsync(() => SeekVideoAsync(time, play));
        return Task.CompletedTask;
    }

    private bool ShouldSyncVideo(BeatmapContext beatmapCtx)
    {
        return VideoElement != null &&
               SharedVm.Default.EnableVideo &&
               ReferenceEquals(_beatmapCtx, beatmapCtx) &&
               !string.IsNullOrWhiteSpace(beatmapCtx?.BeatmapDetail?.VideoPath);
    }

    private async Task PlayVideoFromCurrentPositionAsync()
    {
        var position = _controller.Player?.Position.TotalMilliseconds ?? 0;
        await SeekVideoAsync(position, true);
    }

    private async Task SeekVideoAsync(double audioPosition, bool play)
    {
        var videoPosition = audioPosition + _videoOffset;
        if (videoPosition < 0)
        {
            await PauseVideo();
            VideoElement.Position = TimeSpan.Zero;
            if (!play) return;

            await Task.Delay(TimeSpan.FromMilliseconds(-videoPosition));
            if (_controller.Player?.PlayStatus != PlayStatus.Playing || !ShouldSyncVideo(_beatmapCtx))
                return;

            videoPosition = (_controller.Player?.Position.TotalMilliseconds ?? audioPosition) + _videoOffset;
        }

        VideoElement.Position = TimeSpan.FromMilliseconds(Math.Max(0, videoPosition));
        if (play)
            await PlayVideo();
        else
            await PauseVideo();
    }

    private async void OnMediaOpened(object sender, MediaOpenedEventArgs e)
    {
        VideoElementBorder.Visibility = Visibility.Visible;
        if (!SharedVm.Default.EnableVideo) return;
        if (VideoElement == null) return;
        if (_controller.Player?.PlayStatus == PlayStatus.Playing)
            await PlayVideoFromCurrentPositionAsync();
    }

    private async void OnMediaFailed(object sender, MediaFailedEventArgs e)
    {
        VideoElementBorder.Visibility = Visibility.Hidden;
        Logger.Error(e.ErrorException, "Error while loading video");
        Notification.Push("不支持的视频格式");
        if (!SharedVm.Default.EnableVideo) return;
        await CloseVideoAsync();
    }
}