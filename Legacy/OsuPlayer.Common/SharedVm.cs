using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Common
{
    public class SharedVm : VmBase
    {
        private bool _enableVideo = true;
        private bool _isPlaying = false;

        public bool EnableVideo
        {
            get => _enableVideo;
            set
            {
                if (value == _enableVideo) return;
                _enableVideo = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (value == _isPlaying) return;
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public AppSettings AppSettings { get; } = AppSettings.Default;

        private static SharedVm _default;
        private static object _defaultLock = new object();

        public static SharedVm Default
        {
            get
            {
                lock (_defaultLock)
                {
                    return _default ?? (_default = new SharedVm());
                }
            }
        }

        private SharedVm()
        {
        }
    }
}
