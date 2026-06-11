using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyAsio.Core.Audio;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using OsuPlayer.Core;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Services;
using System.Runtime.Versioning;

namespace OsuPlayer.Views.UserControls;

public partial class PlayControllerVm : ObservableObject
{
    public ObservablePlayController? Controller { get; }
    public SharedVm Shared { get; } = SharedVm.Default;

    [ObservableProperty]
    private Bitmap? _thumbSource;

    [ObservableProperty]
    private double _currentTimeMs;

    [ObservableProperty]
    private double _totalTimeMs = 1;

    [ObservableProperty]
    private string _currentTimeText = "00:00";

    [ObservableProperty]
    private string _totalTimeText = "00:00";

    [ObservableProperty]
    private bool _asioVisible;

    public bool IsDragging { get; set; }

    public PlayControllerVm()
    {
        if (App.Services != null)
        {
            Controller = App.Services.GetService<ObservablePlayController>();

            if (Controller != null)
            {
                Controller.LoadStarted += Controller_LoadStarted;
                Controller.BackgroundInfoLoaded += Controller_BackgroundInfoLoaded;
                Controller.MusicLoaded += Controller_MusicLoaded;
                Controller.PositionUpdated += Controller_PositionUpdated;
                Controller.LoadError += Controller_LoadError;
            }
        }
    }

    private void Controller_LoadError(BeatmapContext ctx, Exception ex)
    {
        Dispatcher.UIThread.Post(() => AppNotificationService.Instance.Push("err-beatmap-load"));
    }

    private void Controller_LoadStarted(BeatmapContext beatmapCtx, CancellationToken ct)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var zero = TimeSpan.Zero.ToString(@"mm\:ss");
            CurrentTimeText = zero;
            TotalTimeText = zero;
            TotalTimeMs = 1;
            CurrentTimeMs = 0;
        });
    }

    private void Controller_BackgroundInfoLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (beatmapCtx.BeatmapDetail?.BackgroundPath is { } path && File.Exists(path))
            {
                try { ThumbSource = new Bitmap(path); } catch { ThumbSource = null; }
            }
            else
            {
                ThumbSource = null;
            }
        });
    }

    private void Controller_MusicLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Controller?.Player == null) return;
            CurrentTimeMs = 0;
            TotalTimeMs = Controller.Player.Duration.TotalMilliseconds;
            TotalTimeText = Controller.Player.Duration.ToString(@"mm\:ss");
            AsioVisible = Controller.Player.Device?.GetType().Name == "AsioOut";
        });
    }

    private void Controller_PositionUpdated(TimeSpan time)
    {
        if (IsDragging) return;
        Dispatcher.UIThread.Post(() =>
        {
            CurrentTimeMs = time.TotalMilliseconds;
            CurrentTimeText = time.ToString(@"mm\:ss");
        });
    }

    [RelayCommand]
    private async Task PrevAsync()
    {
        if (Controller == null) return;
        await Controller.PlayPrevAsync();
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (Controller == null) return;
        await Controller.TogglePlayAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (Controller == null) return;
        await Controller.PlayNextAsync();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (Controller == null) return;

        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "请选择一个.osu文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Osu Files")
                {
                    Patterns = new[] { "*.osu" }
                }
            }
        });

        var path = files.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrEmpty(path)) return;

        await Controller.PlayNewAsync(path);
    }

    [RelayCommand]
    [SupportedOSPlatform("windows")]
    private void Asio()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (Controller?.Player?.Device is AsioOut asio)
        {
            asio.ShowControlPanel();
        }
    }
}

public partial class PlayController : UserControl
{
    public event EventHandler? ThumbClicked;
    public event EventHandler? LikeClicked;

    public PlayController()
    {
        InitializeComponent();

        PlayProgress.AddHandler(Slider.PointerPressedEvent, (_, _) => PlayProgress_DragStarted());
        PlayProgress.AddHandler(Slider.PointerReleasedEvent, (_, _) => PlayProgress_DragCompleted(this, EventArgs.Empty));
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (DataContext is PlayControllerVm vm)
        {
            // Subscribe to play mode close
        }
    }

    private void ThumbButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ThumbClicked?.Invoke(this, EventArgs.Empty);
    }

    private async void PlayProgress_DragCompleted(object? sender, EventArgs e)
    {
        if (DataContext is PlayControllerVm vm)
        {
            try
            {
                if (vm.Controller?.PlayList?.CurrentInfo != null && vm.Controller.Player != null)
                {
                    await vm.Controller.SetTimeAsync(PlayProgress.Value,
                        vm.Controller.Player.PlayStatus == PlayStatus.Playing);
                }
            }
            finally
            {
                vm.IsDragging = false;
            }
        }
    }

    private void PlayProgress_DragStarted()
    {
        if (DataContext is PlayControllerVm vm) vm.IsDragging = true;
    }

    private void ModeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PopMode.IsOpen = true;
    }

    private void LikeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        LikeClicked?.Invoke(this, EventArgs.Empty);
    }

    private void VolumeButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Pop.IsOpen = true;
    }

    private void PlayListButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PopPlayList.IsOpen = true;
    }

    private void PlayListControl_CloseRequested(object? sender, EventArgs e)
    {
        PopPlayList.IsOpen = false;
    }

    private void TitleArtist_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        AppNotificationService.Instance.Push("Title/Artist");
    }
}
