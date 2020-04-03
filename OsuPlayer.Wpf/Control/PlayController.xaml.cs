using Microsoft.Win32;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Windows;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace Milky.OsuPlayer.Control
{
    public class PlayControllerVm : WpfApi.ViewModelBase
    {
        public ObservablePlayController Controller { get; } = Services.Get<ObservablePlayController>();
        public SharedVm Shared { get; } = SharedVm.Default;
    }

    /// <summary>
    /// PlayController.xaml 的交互逻辑
    /// </summary>
    public partial class PlayController : UserControl
    {
        #region Events

        public static readonly RoutedEvent OnThumbClickEvent = EventManager.RegisterRoutedEvent(
            "OnThumbClick",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventArgs<object>),
            typeof(PlayController));

        public event RoutedEventHandler OnThumbClick
        {
            add => AddHandler(OnThumbClickEvent, value);
            remove => RemoveHandler(OnThumbClickEvent, value);
        }

        public static readonly RoutedEvent OnLikeClickEvent = EventManager.RegisterRoutedEvent(
            "OnLikeClick",
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventArgs<object>),
            typeof(PlayController));

        public event RoutedEventHandler OnLikeClick
        {
            add => AddHandler(OnLikeClickEvent, value);
            remove => RemoveHandler(OnLikeClickEvent, value);
        }

        #endregion

        private bool _scrollLock;
        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();

        public PlayController()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            PlayModeControl.CloseRequested += (obj, args) => { PopMode.IsOpen = false; };
            _controller.PreLoadStarted += Controller_PreLoadStarted;
            _controller.LoadStarted += Controller_LoadStarted;
            _controller.BackgroundInfoLoaded += Controller_BackgroundInfoLoaded;
            _controller.MusicLoaded += Controller_MusicLoaded;
            _controller.LoadFinished += Controller_LoadFinished;

            _controller.PositionUpdated += Controller_PositionUpdated;
        }

        private void Controller_PositionUpdated(TimeSpan time)
        {
            if (_scrollLock) return;
            PlayProgress.Value = time.TotalMilliseconds;
            LblNow.Content = time.ToString(@"mm\:ss");
        }

        private void Controller_PreLoadStarted(string path, CancellationToken ct)
        {
        }

        private void Controller_LoadStarted(BeatmapContext beatmapCtx, CancellationToken ct)
        {
            var zero = TimeSpan.Zero.ToString(@"mm\:ss");
            LblNow.Content = zero;
            LblTotal.Content = zero;
            PlayProgress.Maximum = 1;
            PlayProgress.Value = 0;
        }

        private void Controller_BackgroundInfoLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
        {
            Thumb.Source = beatmapCtx.BeatmapDetail.BackgroundPath == null
                ? null
                : new BitmapImage(new Uri(beatmapCtx.BeatmapDetail.BackgroundPath));
        }

        private void Controller_MusicLoaded(BeatmapContext beatmapCtx, CancellationToken ct)
        {
            PlayProgress.Value = 0;
            PlayProgress.Maximum = _controller.Player.Duration.TotalMilliseconds;
            LblTotal.Content = _controller.Player.Duration.ToString(@"mm\:ss");
        }

        private void Controller_LoadFinished(BeatmapContext beatmapCtx, CancellationToken ct)
        {
        }

        private void ThumbButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(OnThumbClickEvent, this));
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            await _controller.PlayPrevAsync();
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            _controller.PlayList.CurrentInfo.TogglePlayHandle();
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

            await _controller.PlayNewAsync(path);
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
        private void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _controller.PlayList.CurrentInfo.SetTimeHandle(PlayProgress.Value,
                _controller.Player.PlayStatus == PlayStatus.Playing);

            _scrollLock = false;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            PopMode.IsOpen = true;
        }

        private void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(OnLikeClickEvent, this));
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
            var win = new BeatmapInfoWindow(_controller.PlayList.CurrentInfo);
            win.ShowDialog();
        }
    }
}