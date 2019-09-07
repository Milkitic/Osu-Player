using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Instances;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;
using Milky.WpfApi.Commands;

namespace Milky.OsuPlayer.Control
{
    public class MiniPlayListControlVm : ViewModelBase
    {
        private PlayerList _playerList;
        private double _positionPercent;
        private PlayerViewModel _player = PlayerViewModel.Current;

        public PlayerList PlayerList
        {
            get => _playerList;
            set
            {
                _playerList = value;
                OnPropertyChanged();
            }
        }
        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }

        public ICommand PlayPrevCommand => new DelegateCommand(async param => await PlayController.Default.PlayPrev());

        public ICommand PlayNextCommand => new DelegateCommand(async param => await PlayController.Default.PlayNext());

        public ICommand PlayPauseCommand => new DelegateCommand(param => PlayController.Default.TogglePlay());

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
        private PlayersInst _playersInst;

        public static event Action MaxButtonClicked;
        public static event Action CloseButtonClicked;

        public MiniPlayController()
        {
            InitializeComponent();
            _viewModel = (MiniPlayListControlVm)DataContext;
            _playersInst = Services.Get<PlayersInst>();
            _viewModel.PlayerList = Services.Get<PlayerList>();
            Default = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _playersInst.AudioPlayer.PositionChanged += AudioPlayer_PositionChanged;
        }

        private void AudioPlayer_PositionChanged(object sender, Media.Audio.Music.ProgressEventArgs e)
        {
            _viewModel.PositionPercent = e.Position / (double)e.Duration;
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            PlayController.Default.OnNewFileLoaded += Default_OnNewFileLoaded;
        }

        private void Default_OnNewFileLoaded(object sender, System.ComponentModel.HandledEventArgs e)
        {
            _playersInst.AudioPlayer.PositionChanged += AudioPlayer_PositionChanged;
            _playersInst.AudioPlayer.PositionSet += AudioPlayer_PositionChanged;
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
    }
}
