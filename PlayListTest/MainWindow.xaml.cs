using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
        private SongInfo _currentInfo;

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

        public SongInfo CurrentInfo
        {
            get => _currentInfo;
            set
            {
                if (Equals(value, _currentInfo)) return;
                _currentInfo = value;
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
        private bool _scrollLock;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _viewModel = (MainWindowVm)DataContext;
            _viewModel.PlayerMixer = _playerMixer;
            _playerMixer.LoadStarted += PlayerMixer_LoadStarted;
            _playerMixer.PlayStatusChanged += PlayerMixer_PlayStatusChanged;
            _playerMixer.ProgressUpdated += PlayerMixer_ProgressUpdated;
            _playerMixer.LoadFinished += PlayerMixer_LoadFinished;
            _playerMixer.InterfaceClearRequest += PlayerMixer_InterfaceClearRequest;
            _playPauseButtonAction = () => _playerMixer.Player?.Play();
        }

        private void PlayerMixer_InterfaceClearRequest()
        {
            Dispatcher?.Invoke(() =>
            {
                PlayerDuration.Content = TimeSpan.Zero.ToString(@"mm\:ss");
                PlayerPlayTime.Content = TimeSpan.Zero.ToString(@"mm\:ss");
                SliderProgress.Maximum = 1;
                SliderProgress.Value = 0;
                _viewModel.CurrentInfo = SongInfo.Logo;
            });
        }

        private void PlayerMixer_LoadStarted(SongInfo obj, CancellationToken token)
        {
            Dispatcher?.Invoke(() =>
            {
                PlayerDuration.Content = TimeSpan.Zero.ToString(@"mm\:ss");
                PlayerPlayTime.Content = TimeSpan.Zero.ToString(@"mm\:ss");
                SliderProgress.Maximum = 1;
                SliderProgress.Value = 0;
                Loading.Visibility = Visibility.Visible;
                PlayPause.IsEnabled = false;
            });
        }

        private void PlayerMixer_LoadFinished(SongInfo obj, CancellationToken token)
        {
            Dispatcher?.Invoke(() =>
            {
                _viewModel.CurrentInfo = obj;
                PlayerDuration.Content = _playerMixer.Player.Duration.ToString(@"mm\:ss");
                SliderProgress.Maximum = _playerMixer.Player.Duration.TotalMilliseconds;
                Loading.Visibility = Visibility.Hidden;
                PlayPause.IsEnabled = true;
            });
        }

        private void PlayerMixer_ProgressUpdated(TimeSpan playTime, TimeSpan duration)
        {
            if (_scrollLock) return;

            Dispatcher?.Invoke(() =>
            {
                if (_scrollLock) return;
                PlayerPlayTime.Content = playTime.ToString(@"mm\:ss");
                SliderProgress.Value = playTime.TotalMilliseconds;
            });
        }

        private void PlayerMixer_PlayStatusChanged(PlayStatus obj)
        {
            if (obj == PlayStatus.Playing)
            {
                _playPauseButtonAction = () => _playerMixer.Player?.Pause();
            }
            else
            {
                _playPauseButtonAction = () => _playerMixer.Player?.Play();
            }

            Dispatcher?.Invoke(() => PlayPause.Content = obj == PlayStatus.Playing ? "STOP" : "PLAY");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fi = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var files = fi.GetFiles().Select(k => new SongInfo { Title = k.Name });
            //_viewModel.PlayerMixer.PlayList.SongList.Add(new SongInfo { Title = "haha" });
            _playerMixer.PlayList.SetSongList(new ObservableCollection<SongInfo>(files), true);
        }

        private void ManualPrev_Click(object sender, RoutedEventArgs e)
        {
            var result = _playerMixer.PrevAsync();
            Console.WriteLine(result);
        }

        private void ManualNext_Click(object sender, RoutedEventArgs e)
        {
            var result = _playerMixer.NextAsync();
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

        private void SliderProgress_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _scrollLock = true;
        }

        private void SliderProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            var progress = (int)SliderProgress.Value;
            _playerMixer.Player.SkipTo(progress);
            _scrollLock = false;
        }

        private void SliderProgress_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            PlayerPlayTime.Content = TimeSpan.FromMilliseconds(SliderProgress.Value).ToString(@"mm\:ss");
        }
    }
}
