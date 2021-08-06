using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace Milky.OsuPlayer.UserControls
{
    public class MiniPlayListControlVm : VmBase
    {
        private ObservablePlayController _controller;
        private double _positionPercent;

        public ObservablePlayController Controller
        {
            get => _controller;
            set
            {
                _controller = value;
                OnPropertyChanged();
            }
        }

        public SharedVm Shared { get; } = SharedVm.Default;

        public ICommand PlayPrevCommand => new DelegateCommand(async param => await _controller.PlayPrevAsync());

        public ICommand PlayNextCommand => new DelegateCommand(async param => await _controller.PlayNextAsync());

        public ICommand PlayPauseCommand => new DelegateCommand(param => _controller.Player.TogglePlay());

        public double PositionPercent
        {
            get => _positionPercent;
            set
            {
                _positionPercent = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// MiniPlayController.xaml 的交互逻辑
    /// </summary>
    public partial class MiniPlayController : UserControl
    {
        private MiniPlayListControlVm _viewModel;

        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        public static event Action MaxButtonClicked;
        public static event Action CloseButtonClicked;

        public MiniPlayController()
        {
            InitializeComponent();
            _viewModel = (MiniPlayListControlVm)DataContext;
            _viewModel.Controller = Service.Get<ObservablePlayController>();
            Default = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlayModeControl.CloseRequested += (obj, args) => PopMode.IsOpen = false;
            if (_controller != null)
                _controller.PositionUpdated += AudioPlayer_PositionChanged;
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            //if (_controller != null) _controller.LoadFinished += Controller_LoadFinished;
        }

        private void Controller_LoadFinished(BeatmapContext arg1, System.Threading.CancellationToken arg2)
        {
            //_controller.PositionUpdated += AudioPlayer_PositionChanged;
        }

        private void AudioPlayer_PositionChanged(TimeSpan time)
        {
            _viewModel.PositionPercent = time.TotalMilliseconds / _controller.Player.Duration.TotalMilliseconds;
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            MaxButtonClicked?.Invoke();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseButtonClicked?.Invoke();
        }

        public static MiniPlayController Default { get; private set; }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            PopVolume.IsOpen = true;
        }

        private void PlayListControl_CloseRequested(object sender, RoutedEventArgs e)
        {
            PopPlayList.IsOpen = false;
        }

        private void PlayListButton_Click(object sender, RoutedEventArgs e)
        {
            PopPlayList.IsOpen = true;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            PopMode.IsOpen = true;
        }

        private async void ButtonLike_Click(object sender, RoutedEventArgs e)
        {
            await using var dbContext = new ApplicationDbContext();

            var collection = await dbContext.Collections.FirstOrDefaultAsync(k => k.IsDefault);
            var metadata = _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata;
            if (metadata.IsFavorite)
            {
                await dbContext.DeleteBeatmapFromCollection(_controller.PlayList.CurrentInfo.Beatmap, collection);
                metadata.IsFavorite = false;
            }
            else
            {
                if (await SelectCollectionControl.AddToCollectionAsync(collection,
                    new[]
                    {
                        _controller.PlayList.CurrentInfo.Beatmap
                    }))
                    metadata.IsFavorite = true;
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            OsuBg.Visibility = Visibility.Visible;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            OsuBg.Visibility = Visibility.Hidden;
        }

        private void BgRetc_MouseMove(object sender, MouseEventArgs e)
        {
            //OsuBg.Opacity = 0;
            //RectCover.Opacity = 0.8;
            //BgBorder.Opacity = 1;
            //BlurEffect.Radius = 0;
        }

        private void BgRetc_MouseLeave(object sender, MouseEventArgs e)
        {
            //OsuBg.Opacity = 1;
            //RectCover.Opacity = 1;
            //BgBorder.Opacity = 0;
            //BlurEffect.Radius = 20;
        }
    }
}
