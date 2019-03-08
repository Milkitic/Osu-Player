using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Windows
{
    internal class LyricWindowViewModel : ViewModelBase
    {
        private PlayerViewModel _player;

        public PlayerViewModel Player
        {
            get => _player;
            set
            {
                _player = value;
                OnPropertyChanged();
            }
        }
    }
}