using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Windows;
using NAudio.Wave;

namespace Milki.OsuPlayer.UserControls;

public class PlayControllerVm : VmBase
{
    public PlayerService PlayerService { get; } = ServiceProviders.Default.GetService<PlayerService>();
    public SharedVm Shared { get; } = SharedVm.Default;
}

/// <summary>
/// PlayController.xaml 的交互逻辑
/// </summary>
public partial class PlayController : UserControl
{
    #region Events

    public static readonly RoutedEvent ThumbClickedEvent = EventManager.RegisterRoutedEvent(
        "ThumbClicked",
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventArgs<object>),
        typeof(PlayController));

    public event RoutedEventHandler ThumbClicked
    {
        add => AddHandler(ThumbClickedEvent, value);
        remove => RemoveHandler(ThumbClickedEvent, value);
    }

    public static readonly RoutedEvent LikeClickedEvent = EventManager.RegisterRoutedEvent(
        "LikeClicked",
        RoutingStrategy.Bubble,
        typeof(RoutedPropertyChangedEventArgs<object>),
        typeof(PlayController));

    public event RoutedEventHandler LikeClicked
    {
        add => AddHandler(LikeClickedEvent, value);
        remove => RemoveHandler(LikeClickedEvent, value);
    }

    #endregion

    private bool _scrollLock;
    private readonly PlayerService _controller;
    private IWavePlayer _device;

    public PlayController()
    {
        _controller = ServiceProviders.Default.GetService<PlayerService>()!;

        _controller.LoadStarted += Controller_LoadStarted;
        _controller.LoadBackgroundInfoFinished += Controller_BackgroundInfoLoaded;
        _controller.LoadMusicFinished += Controller_MusicLoaded;

        _controller.PlayTimeChanged += Controller_PlayTimeChanged;

        InitializeComponent();
    }

    private void UserControl_Initialized(object sender, EventArgs e)
    {
        PlayModeControl.CloseRequested += (obj, args) => { PopMode.IsOpen = false; };
    }

    private ValueTask Controller_LoadStarted(PlayerService.PlayItemLoadContext loadContext)
    {
        var zero = TimeSpan.Zero.ToString(@"mm\:ss");
        LblNow.Content = zero;
        LblTotal.Content = zero;
        PlayProgress.Maximum = 1;
        PlayProgress.Value = 0;
        return ValueTask.CompletedTask;
    }

    private void Controller_PlayTimeChanged(TimeSpan time)
    {
        if (_scrollLock) return;
        PlayProgress.Value = time.TotalMilliseconds;
        LblNow.Content = time.ToString(@"mm\:ss");
    }

    private ValueTask Controller_BackgroundInfoLoaded(PlayerService.PlayItemLoadContext loadContext)
    {
        Thumb.Source = loadContext.BackgroundPath == null ? null : new BitmapImage(new Uri(loadContext.BackgroundPath));
        return ValueTask.CompletedTask;
    }

    private ValueTask Controller_MusicLoaded(PlayerService.PlayItemLoadContext loadContext)
    {
        PlayProgress.Value = 0;
        PlayProgress.Maximum = loadContext.Player!.Duration;
        LblTotal.Content = TimeSpan.FromMilliseconds(loadContext.Player!.Duration).ToString(@"mm\:ss");
        return ValueTask.CompletedTask;
    }

    private void ThumbButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ThumbClickedEvent, this));
    }

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        await _controller.PlayPreviousAsync();
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        await _controller.TogglePlayAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        await _controller.PlayNextAsync();
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = @"请选择一个.osu文件",
            Filter = @"Osu Files(*.osu)|*.osu"
        };
        var result = openFileDialog.ShowDialog();
        var path = result == true ? openFileDialog.FileName : null;
        if (path == null) return;

        await _controller.InitializeNewAsync(path, true);
    }

    /// <summary>
    /// Play progress control.
    /// While drag started, slider's updating should be paused.
    /// </summary>
    private void PlayProgress_DragStarted(object sender, DragStartedEventArgs e)
    {
        _scrollLock = true;
    }

    /// <summary>
    /// Play progress control.
    /// While drag ended, slider's updating should be recovered.
    /// </summary>
    private async void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        await _controller.SeekAsync(TimeSpan.FromMilliseconds(PlayProgress.Value));
        _scrollLock = false;
    }

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        PopMode.IsOpen = true;
    }

    private void LikeButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(LikeClickedEvent, this));
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        Pop.IsOpen = true;
    }

    private void PlayListButton_Click(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = true;
    }

    private void PlayListControl_CloseRequested(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = false;
    }

    private void TitleArtist_Click(object sender, RoutedEventArgs e)
    {
        if (_controller.LastLoadContext?.PlayItem is { PlayItemDetail: { } detail })
        {
            var win = new BeatmapInfoWindow(detail);
            win.ShowDialog();
        }
    }

    private void BtnAsio_OnClick(object sender, RoutedEventArgs e)
    {
        if (_device is AsioOut asio)
        {
            asio.ShowControlPanel();
        }
    }
}