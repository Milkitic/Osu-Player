using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio;
using Milky.WpfApi;

namespace Milky.OsuPlayer.ViewModels
{
    public class LyricWindowViewModel : ViewModelBase
    {
        private bool _showFrame;
        private bool _isLyricEnabled;

        public ObservablePlayController Controller { get; } = Services.Get<ObservablePlayController>();
        public SharedVm Shared { get; } = SharedVm.Default;

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