using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using PlayListTest.Annotations;
using PlayListTest.Models;

namespace PlayListTest
{
    public class MainWindowVm : INotifyPropertyChanged
    {
        private ObservablePlayerMixer _playerMixer;

        public ObservablePlayerMixer PlayerMixer
        {
            get => _playerMixer;
            set
            {
                if (Equals(value, _playerMixer)) return;
                _playerMixer = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservablePlayerMixer _playerMixer = new ObservablePlayerMixer();
        private MainWindowVm _viewModel;

        private Action _playPauseButtonAction;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _viewModel = (MainWindowVm)DataContext;
            _viewModel.PlayerMixer = _playerMixer;
            _playerMixer.PlayStatusChanged += PlayerMixer_PlayStatusChanged;
            _playerMixer.ProgressUpdated += PlayerMixer_ProgressUpdated;
            _playerMixer.LoadFinished += PlayerMixer_LoadFinished;
            _playPauseButtonAction = () => _playerMixer.Player?.Play();
        }

        private void PlayerMixer_LoadFinished(SongInfo obj)
        {
            Dispatcher?.Invoke(() =>
            {
                PlayerDuration.Content = _playerMixer.Player.Duration.ToString(@"mm\:ss");
                SliderProgress.Maximum = _playerMixer.Player.Duration.TotalMilliseconds;
            });
        }

        private void PlayerMixer_ProgressUpdated(TimeSpan playTime, TimeSpan duration)
        {
            Dispatcher?.Invoke(() =>
            {
                PlayerPlayTime.Content = playTime.ToString(@"mm\:ss");
                SliderProgress.Value = playTime.TotalMilliseconds;
            });
        }

        private void PlayerMixer_PlayStatusChanged(PlayStatus obj)
        {
            if (obj == PlayStatus.Playing)
            {
                _playPauseButtonAction = () => _playerMixer.Player?.Pause();
                PlayPause.Content = "STOP";
            }
            else
            {
                _playPauseButtonAction = () => _playerMixer.Player?.Play();
                PlayPause.Content = "PLAY";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fi = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var files = fi.GetFiles().Select(k => new SongInfo { Title = k.Name });
            //_viewModel.PlayerMixer.PlayList.SongList.Add(new SongInfo { Title = "haha" });
            _viewModel.PlayerMixer.PlayList.SetSongList(new ObservableCollection<SongInfo>(files), true);
        }

        private void ManualPrev_Click(object sender, RoutedEventArgs e)
        {
            var result = _playerMixer.PlayList.SwitchByControl(PlayControl.Previous);
            Console.WriteLine(result);
        }

        private void ManualNext_Click(object sender, RoutedEventArgs e)
        {
            var result = _playerMixer.PlayList.SwitchByControl(PlayControl.Next);
            Console.WriteLine(result);
        }

        private void AutoPrev_Click(object sender, RoutedEventArgs e)
        {
            //var result = await _playList.SwitchToAsync(false, false);
            //Console.WriteLine(result);
        }

        private void AutoNext_Click(object sender, RoutedEventArgs e)
        {
            //var result = await _playList.SwitchToAsync(true, false);
            //Console.WriteLine(result);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ListSong.SelectedIndex = (int)e.AddedItems[0];
            }
        }

        private void BtnAppend_Click(object sender, RoutedEventArgs e)
        {
            var text = AddText.Text;
            text = text.Trim();
            AddText.Clear();
            if (!string.IsNullOrEmpty(text))
                _playerMixer.PlayList.SongList.Add(new SongInfo { Title = text });
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var o = (SongInfo)ListSong.SelectedItem;
            if (o != null) _playerMixer.PlayList.SongList.Remove(o);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            _playPauseButtonAction?.Invoke();
        }
    }
}
