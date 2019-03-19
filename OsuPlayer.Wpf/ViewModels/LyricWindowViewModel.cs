using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Windows
{
    internal class LyricWindowViewModel : ViewModelBase
    {
        private PlayerViewModel _player;
        private bool _showFrame;

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
    }
}