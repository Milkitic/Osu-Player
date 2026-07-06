using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NAudio.Wave;
using OsuPlayer.Core;
using OsuPlayer.Media.Audio;
using OsuPlayer.Playback;
using OsuPlayer.Playback.Playlist;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.UiComponents.NotificationComponent;
using OsuPlayer.Utils;
using OsuPlayer.Windows;

namespace OsuPlayer.UserControls;

public partial class PlayControllerVm : ObservableObject
{
    public ObservablePlayController Controller { get; }
    public SharedVm Shared { get; } = SharedVm.Default;

    [ObservableProperty]
    public partial ImageSource ThumbSource { get; set; }

    [ObservableProperty]
    public partial double CurrentTimeMs { get; set; }

    [ObservableProperty]
    public partial double TotalTimeMs { get; set; } = 1;

    [ObservableProperty]
    public partial string CurrentTimeText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string TotalTimeText { get; set; } = "00:00";

    [ObservableProperty]
    public partial Visibility AsioVisible { get; set; } = Visibility.Collapsed;

    public bool IsDragging { get; set; }

    public PlayControllerVm()
    {
        if (App.Services != null)
        {
            Controller = App.Services.GetRequiredService<ObservablePlayController>();

            Controller.PreLoadStarted += Controller_PreLoadStarted;
            Controller.LoadStarted += Controller_LoadStarted;
            Controller.BackgroundInfoLoaded += Controller_BackgroundInfoLoaded;
            Controller.MusicLoaded += Controller_MusicLoaded;
            Controller.PositionUpdated += Controller_PositionUpdated;
            Controller.LoadError += Controller_LoadError;
        }
    }

    private void Controller_LoadError(BeatmapContext ctx, Exception ex)
    {
        if (ctx?.BeatmapDetail != null)
        {
            var path = Path.Combine(ctx.BeatmapDetail.BaseFolder ?? "", ctx.BeatmapDetail.MapPath ?? "");
            Notification.Push($"{I18NUtil.GetString("err-beatmap-load")}: {path}");
        }
        else
        {
            Notification.Push(I18NUtil.GetString("err-beatmap-load"));
        }
    }

    private void Controller_PreLoadStarted(string path, CancellationToken ct)
    {
    }

    private void Controller_LoadStarted(BeatmapContext beatmapCtx, CancellationToken ct)
    {
        Execute.OnUiThread(() =>
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
        Execute.OnUiThread(() =>
        {
            ThumbSource = beatmapCtx.BeatmapDetail.BackgroundPath == null
                ? null
                : new BitmapImage(new Uri(beatmapCtx.BeatmapDetail.BackgroundPath));
        });
    }

    private void Controller_MusicLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
    {
        Execute.OnUiThread(() =>
        {
            CurrentTimeMs = 0;
            TotalTimeMs = Controller.Player.Duration.TotalMilliseconds;
            TotalTimeText = Controller.Player.Duration.ToString(@"mm\:ss");

            var device = Controller.Player.Device;
            AsioVisible = device is AsioOut ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private void Controller_PositionUpdated(TimeSpan time)
    {
        if (IsDragging) return;
        Execute.OnUiThread(() =>
        {
            CurrentTimeMs = time.TotalMilliseconds;
            CurrentTimeText = time.ToString(@"mm\:ss");
        });
    }

    [RelayCommand]
    private async Task PrevAsync()
    {
        await Controller.PlayPrevAsync();
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        await Controller.TogglePlayAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        await Controller.PlayNextAsync();
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = @"请选择一个.osu文件",
            Filter = @"Osu Files(*.osu)|*.osu"
        };
        var result = openFileDialog.ShowDialog();
        var path = result == true ? openFileDialog.FileName : null;
        if (path == null) return;

        await Controller.PlayNewAsync(path);
    }

    [RelayCommand]
    private void Asio()
    {
        if (Controller.Player?.Device is AsioOut asio)
        {
            asio.ShowControlPanel();
        }
    }
}

/// <summary>
/// PlayController.xaml 的交互逻辑
/// </summary>
public partial class PlayController : UserControl
{
    #region Events

    /// <summary>
    /// Occurs when the thumbnail (cover art) is clicked.
    /// </summary>
    public event EventHandler? ThumbClicked;

    /// <summary>
    /// Occurs when the like/favorite button is clicked.
    /// </summary>
    public event EventHandler? LikeClicked;

    #endregion

    public PlayController()
    {
        InitializeComponent();
    }

    private void UserControl_Initialized(object sender, EventArgs e)
    {
        PlayModeControl.CloseRequested += (obj, args) => { PopMode.IsOpen = false; };
    }

    private void ThumbButton_Click(object sender, RoutedEventArgs e)
    {
        ThumbClicked?.Invoke(this, EventArgs.Empty);
    }

    private void PlayProgress_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (DataContext is PlayControllerVm vm)
        {
            vm.IsDragging = true;
        }
    }

    private async void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is PlayControllerVm vm)
        {
            try
            {
                var player = vm.Controller.Player;
                if (vm.Controller.PlayList?.CurrentInfo != null && player != null)
                {
                    await vm.Controller.SetTimeAsync(PlayProgress.Value,
                        player.PlayStatus == PlayStatus.Playing);
                }
            }
            finally
            {
                vm.IsDragging = false;
            }
        }
    }

    private void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        PopMode.IsOpen = true;
    }

    private void LikeButton_Click(object sender, RoutedEventArgs e)
    {
        LikeClicked?.Invoke(this, EventArgs.Empty);
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        Pop.IsOpen = true;
    }

    private void PlayListButton_Click(object sender, RoutedEventArgs e)
    {
        PopPlayList.IsOpen = true;
    }

    private void PlayListControl_CloseRequested(object sender, EventArgs e)
    {
        PopPlayList.IsOpen = false;
    }

    private void TitleArtist_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PlayControllerVm vm && vm.Controller.PlayList?.CurrentInfo != null)
        {
            var win = new BeatmapInfoWindow(vm.Controller.PlayList.CurrentInfo);
            win.ShowDialog();
        }
    }
}
