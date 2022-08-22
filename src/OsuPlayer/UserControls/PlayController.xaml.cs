using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Windows;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.UserControls;

public class PlayControllerVm : VmBase
{
    private TimeSpan _maxTime;
    private TimeSpan _minTime;
    private TimeSpan _currentTime;
    private double _maxTimeMs;
    private double _minTimeMs;
    private double _currentTimeMs;

    public TimeSpan MaxTime
    {
        get => _maxTime;
        set => this.RaiseAndSetIfChanged(ref _maxTime, value);
    }

    public TimeSpan MinTime
    {
        get => _minTime;
        set => this.RaiseAndSetIfChanged(ref _minTime, value);
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    public double MaxTimeMs
    {
        get => _maxTimeMs;
        set => this.RaiseAndSetIfChanged(ref _maxTimeMs, value);
    }

    public double MinTimeMs
    {
        get => _minTimeMs;
        set => this.RaiseAndSetIfChanged(ref _minTimeMs, value);
    }

    public double CurrentTimeMs
    {
        get => _currentTimeMs;
        set => this.RaiseAndSetIfChanged(ref _currentTimeMs, value);
    }

    public PlayerService PlayerService { get; } = ServiceProviders.Default.GetService<PlayerService>();
    public PlayListService PlayListService { get; } = ServiceProviders.Default.GetService<PlayListService>();
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
    private readonly PlayerService _playerService;
    private readonly PlayControllerVm _viewModel;

    public PlayController()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playerService = ServiceProviders.Default.GetService<PlayerService>()!;
            _playerService.LoadStarted += PlayerService_LoadStarted;
            _playerService.LoadBackgroundInfoFinished += PlayerService_BackgroundInfoLoaded;
            _playerService.LoadMusicFinished += PlayerService_MusicLoaded;
            _playerService.PlayTimeChanged += PlayerService_PlayTimeChanged;
        }

        DataContext = _viewModel = new PlayControllerVm();
        InitializeComponent();
    }

    private void UserControl_Initialized(object sender, EventArgs e)
    {
        PlayModeControl.CloseRequested += (obj, args) => { PopMode.IsOpen = false; };
    }

    private ValueTask PlayerService_LoadStarted(PlayerService.PlayItemLoadContext loadContext)
    {
        Execute.OnUiThread(() =>
        {
            _viewModel.MaxTime = TimeSpan.Zero;
            _viewModel.MaxTimeMs = 0;
            _viewModel.MinTime = TimeSpan.Zero;
            _viewModel.MinTimeMs = 0;
            _viewModel.CurrentTime = TimeSpan.Zero;
            _viewModel.CurrentTimeMs = 0;
        });
        return ValueTask.CompletedTask;
    }

    private void PlayerService_PlayTimeChanged(TimeSpan time)
    {
        if (_scrollLock) return;
        Execute.OnUiThread(() =>
        {
            _viewModel.CurrentTime = time;
            _viewModel.CurrentTimeMs = time.TotalMilliseconds;
        });
    }

    private ValueTask PlayerService_BackgroundInfoLoaded(PlayerService.PlayItemLoadContext loadContext)
    {
        Thumb.Source = loadContext.BackgroundPath == null ? null : new BitmapImage(new Uri(loadContext.BackgroundPath));
        return ValueTask.CompletedTask;
    }

    private ValueTask PlayerService_MusicLoaded(PlayerService.PlayItemLoadContext loadContext)
    {
        var player = loadContext.Player!;
        Execute.OnUiThread(() =>
        {
            _viewModel.MinTime = TimeSpan.FromMilliseconds(-player.PreInsertDuration);
            _viewModel.MinTimeMs = -player.PreInsertDuration;
            _viewModel.MaxTime = player.TotalTime;
            _viewModel.MaxTimeMs = player.TotalTime.TotalMilliseconds;
            _viewModel.CurrentTime = player.PlayTime;
            _viewModel.CurrentTimeMs = player.PlayTime.TotalMilliseconds;
        });

        return ValueTask.CompletedTask;
    }

    private void ThumbButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ThumbClickedEvent, this));
    }

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        await _playerService.PlayPreviousAsync();
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        await _playerService.TogglePlayAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        await _playerService.PlayNextAsync();
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

        await _playerService.InitializeNewAsync(path, true);
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
        var slider = (Slider)e.OriginalSource;
        await _playerService.SeekAsync(TimeSpan.FromMilliseconds(slider.Value));
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
        if (_playerService.LastLoadContext?.PlayItem is { PlayItemDetail: { } detail })
        {
            var win = new BeatmapInfoWindow(detail);
            win.ShowDialog();
        }
    }
}