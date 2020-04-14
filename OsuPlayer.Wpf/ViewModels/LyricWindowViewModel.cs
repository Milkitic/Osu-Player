using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Dependency;

namespace Milky.OsuPlayer.ViewModels
{
    public class LyricWindowViewModel : VmBase
    {
        private bool _showFrame;
        private bool _isLyricEnabled;

        public ObservablePlayController Controller { get; } = Service.Get<ObservablePlayController>();
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