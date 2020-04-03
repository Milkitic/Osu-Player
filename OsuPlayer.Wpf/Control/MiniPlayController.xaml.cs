using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milky.OsuPlayer.Control
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
        private AppDbOperator _appDbOperator = new AppDbOperator();

        private readonly ObservablePlayController _controller = Services.Get<ObservablePlayController>();

        public static event Action MaxButtonClicked;
        public static event Action CloseButtonClicked;

        public MiniPlayController()
        {
            InitializeComponent();
            _viewModel = (MiniPlayListControlVm)DataContext;
            _viewModel.Controller = Services.Get<ObservablePlayController>();
            Default = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlayModeControl.CloseRequested += (obj, args) => PopMode.IsOpen = false;
            _controller.Player.PositionUpdated += AudioPlayer_PositionChanged;
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _controller.LoadFinished += Controller_LoadFinished;
        }

        private void Controller_LoadFinished(BeatmapContext arg1, System.Threading.CancellationToken arg2)
        {
            _controller.Player.PositionUpdated += AudioPlayer_PositionChanged;
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

        private async void CommonButton_Click(object sender, RoutedEventArgs e)
        {
            var collection = _appDbOperator.GetCollections().First(k => k.LockedBool);
            var metadata = _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata;
            if (metadata.IsFavorite)
            {
                _appDbOperator.RemoveMapFromCollection(_controller.PlayList.CurrentInfo.Beatmap, collection);
                metadata.IsFavorite = false;
            }
            else
            {
                await SelectCollectionControl.AddToCollectionAsync(collection,
                    new[]
                    {
                        _controller.PlayList.CurrentInfo.Beatmap
                    });
                metadata.IsFavorite = true;
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Bg.Visibility = Visibility.Visible;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            Bg.Visibility = Visibility.Hidden;
        }
    }
}
