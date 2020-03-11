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
        private PlayList _playList;

        public PlayList PlayList
        {
            get => _playList;
            set
            {
                if (Equals(value, _playList)) return;
                _playList = value;
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
        PlayList _playList = new PlayList();
        private MainWindowVm _viewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _viewModel = (MainWindowVm)DataContext;
            _viewModel.PlayList = _playList;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fi = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            var files = fi.GetFiles().Select(k => new SongInfo { Title = k.Name });
            _viewModel.PlayList.SongList.Add(new SongInfo() { Title = "haha" });
            await _viewModel.PlayList.SetSongListAsync(new ObservableCollection<SongInfo>(files), true);
        }

        private async void ManualPrev_Click(object sender, RoutedEventArgs e)
        {
            await _playList.SwitchToAsync(false, true);
        }

        private async void ManualNext_Click(object sender, RoutedEventArgs e)
        {
            await _playList.SwitchToAsync(true, true);
        }

        private async void AutoPrev_Click(object sender, RoutedEventArgs e)
        {
            await _playList.SwitchToAsync(false, false);
        }

        private async void AutoNext_Click(object sender, RoutedEventArgs e)
        {
            await _playList.SwitchToAsync(true, false);
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
                _playList.SongList.Add(new SongInfo { Title = text });
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var o = (SongInfo)ListSong.SelectedItem;
            if (o != null)
            {
                _playList.SongList.Remove(o);
            }
        }
    }
}
