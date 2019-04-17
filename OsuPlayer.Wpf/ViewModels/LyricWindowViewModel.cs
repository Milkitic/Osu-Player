using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Windows
{
    public class LyricWindowViewModel : ViewModelBase
    {
        private PlayerViewModel _player;
        private bool _showFrame;
        private bool _isLyricEnabled;

        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFrame
        {
            get => _showFrame;
            set
            {
                _showFrame = value;
                OnPropertyChanged();
            }
        }


        public bool IsLyricWindowShown
        {
            get => _isLyricEnabled;
            set
            {
                _isLyricEnabled = value;
                OnPropertyChanged();
            }
        }
    }
}