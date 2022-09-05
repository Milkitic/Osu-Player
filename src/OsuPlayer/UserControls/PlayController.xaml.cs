using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.ContentDialogComponent;
using Milki.OsuPlayer.Windows;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.UserControls;

public class PlayControllerVm : VmBase
{
    private double _maxTime = 0.001;
    private double _minTime;
    private double _currentTime;
    private string _title;
    private string _artist;
    private bool _isPlayerEmpty = true;
    private bool _isPlayerLoading;
    private bool _isPlayerError;

    public double MaxTime
    {
        get => _maxTime;
        set => this.RaiseAndSetIfChanged(ref _maxTime, value);
    }

    public double MinTime
    {
        get => _minTime;
        set => this.RaiseAndSetIfChanged(ref _minTime, value);
    }

    public double CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string Artist
    {
        get => _artist;
        set => this.RaiseAndSetIfChanged(ref _artist, value);
    }

    public bool IsPlayerEmpty
    {
        get => _isPlayerEmpty;
        set => this.RaiseAndSetIfChanged(ref _isPlayerEmpty, value);
    }

    public bool IsPlayerLoading
    {
        get => _isPlayerLoading;
        set => this.RaiseAndSetIfChanged(ref _isPlayerLoading, value);
    }

    public bool IsPlayerError
    {
        get => _isPlayerError;
        set => this.RaiseAndSetIfChanged(ref _isPlayerError, value);
    }

    public PlayerService PlayerService { get; } = ServiceProviders.Default?.GetService<PlayerService>();
    public PlayListService PlayListService { get; } = ServiceProviders.Default?.GetService<PlayListService>();
}

/// <summary>
/// PlayController.xaml 的交互逻辑
/// </summary>
public partial class PlayController : UserControl
{
    public event Action ToggleAnimationSceneRequested;

    private bool _scrollLock;
    private readonly PlayerService _playerService;
    private readonly PlayControllerVm _viewModel;

    public PlayController()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            _playerService = ServiceProviders.Default.GetService<PlayerService>()!;
            _playerService.LoadMetaFinished += PlayerService_OnLoadMetaFinished;
            _playerService.LoadStarted += PlayerService_OnLoadStarted;
            _playerService.LoadBackgroundInfoFinished += PlayerService_OnBackgroundInfoLoaded;
            _playerService.LoadMusicFinished += PlayerService_OnMusicLoaded;
            _playerService.PlayTimeChanged += PlayerService_OnPlayTimeChanged;

            DataContext = _viewModel = new PlayControllerVm();
        }

        InitializeComponent();
    }

    private void UserControl_Initialized(object sender, EventArgs e)
    {
        PlayModeControl.CloseRequested += (_, _) => { PopMode.IsOpen = false; };
    }

    private void BtnThumb_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleAnimationSceneRequested?.Invoke();
    }

    private async void BtnPrev_OnClick(object sender, RoutedEventArgs e)
    {
        await _playerService.PlayPreviousAsync();
    }

    private async void BtnPlay_OnClick(object sender, RoutedEventArgs e)
    {
        await _playerService.TogglePlayAsync();
    }

    private async void BtnNext_OnClick(object sender, RoutedEventArgs e)
    {
        await _playerService.PlayNextAsync();
    }

    private async void BtnOpen_OnClick(object sender, RoutedEventArgs e)
    {
        using var openFileDialog = new CommonOpenFileDialog
        {
            Title = "请选择一个图片",
            Filters = { new CommonFileDialogFilter("Osu Files", "osu") }
        };
        var result = openFileDialog.ShowDialog();
        var path = result == CommonFileDialogResult.Ok ? openFileDialog.FileName : null;
        if (path == null) return;

        await _playerService.InitializeNewAsync(path, true);
    }

    private void BtnPlayListMode_OnClick(object sender, RoutedEventArgs e)
    {
        PopMode.IsOpen = true;
    }

    private void BtnLike_OnClick(object sender, RoutedEventArgs e)
    {
        if (_playerService.LastLoadContext is { PlayItem: { } playItem })
        {
            App.CurrentMainContentDialog.ShowContent(new SelectPlayListControl(playItem),
                DialogOptionFactory.SelectPlayListOptions);
        }
    }

    private void BtnVolume_OnClick(object sender, RoutedEventArgs e)
    {
        Pop.IsOpen = true;
    }

    private void BtnCurrentPlay_OnClick(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = true;
    }

    private void BtnMetadata_OnClick(object sender, RoutedEventArgs e)
    {
        if (_playerService.LastLoadContext?.PlayItem is { PlayItemDetail: { } detail })
        {
            var win = new BeatmapInfoWindow(detail);
            win.ShowDialog();
        }
    }

    private void CurrentPlayControl_OnCloseRequested(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = false;
    }

    /// <summary>
    /// Play progress control.
    /// While drag started, slider's updating should be paused.
    /// </summary>
    private void PlayProgress_OnDragStarted(object sender, DragStartedEventArgs e)
    {
        _scrollLock = true;
    }

    /// <summary>
    /// Play progress control.
    /// While drag ended, slider's updating should be recovered.
    /// </summary>
    private async void PlayProgress_OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        var slider = (Slider)sender;
        await _playerService.SeekAsync(TimeSpan.FromMilliseconds(slider.Value));
        _scrollLock = false;
    }

    private ValueTask PlayerService_OnLoadMetaFinished(PlayerService.PlayItemLoadContext arg)
    {
        _viewModel.Artist = arg.PlayItem?.PlayItemDetail.AutoArtist;
        _viewModel.Title = arg.PlayItem?.PlayItemDetail.AutoTitle;
        return ValueTask.CompletedTask;
    }

    private ValueTask PlayerService_OnLoadStarted(PlayerService.PlayItemLoadContext loadContext)
    {
        Execute.OnUiThread(() =>
        {
            _viewModel.MaxTime = 0.001;
            _viewModel.MinTime = 0;
            _viewModel.CurrentTime = 0;
            _viewModel.IsPlayerLoading = true;
            _viewModel.IsPlayerEmpty = false;
        });
        return ValueTask.CompletedTask;
    }

    private ValueTask PlayerService_OnBackgroundInfoLoaded(PlayerService.PlayItemLoadContext loadContext)
    {
        Execute.OnUiThread(() =>
        {
            Thumb.Source = loadContext.BackgroundPath == null
                ? null
                : new BitmapImage(new Uri(loadContext.BackgroundPath));
        });
        return ValueTask.CompletedTask;
    }

    private ValueTask PlayerService_OnMusicLoaded(PlayerService.PlayItemLoadContext loadContext)
    {
        var player = loadContext.Player!;
        Execute.OnUiThread(() =>
        {
            _viewModel.MinTime = -player.PreInsertDuration;
            _viewModel.MaxTime = player.TotalTime.TotalMilliseconds;
            _viewModel.CurrentTime = player.PlayTime.TotalMilliseconds;
            _viewModel.IsPlayerLoading = false;
        });

        return ValueTask.CompletedTask;
    }

    private void PlayerService_OnPlayTimeChanged(TimeSpan time)
    {
        if (_scrollLock) return;
        Execute.OnUiThread(() =>
        {
            _viewModel.CurrentTime = time.TotalMilliseconds;
        });
    }
}