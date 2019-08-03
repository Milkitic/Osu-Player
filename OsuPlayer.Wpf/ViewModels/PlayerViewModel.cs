using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class PlayerViewModel : ViewModelBase
    {
        public static PlayerViewModel Current { get; set; }

        public static void InitViewModel()
        {
            if (Current != null)
                return;
            Current = new PlayerViewModel();
        }

        private PlayerViewModel()
        {
            CurrentVolume = AppSettings.Current.Volume;
        }

        private bool _isPlaying;
        private bool _enableVideo;
        private PlayListMode _playListMode;

        private long _position;
        private long _duration;
        private CurrentInfo _currentInfo;
        private VolumeControl _currentVolume;

        public PlayListMode PlayListMode
        {
            get => _playListMode;
            set
            {
                _playListMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public bool EnableVideo
        {
            get => _enableVideo;
            set
            {
                _enableVideo = value;
                OnPropertyChanged();
            }
        }

        public long Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }

        public long Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        public CurrentInfo CurrentInfo
        {
            get => _currentInfo;
            set
            {
                _currentInfo = value;
                OnPropertyChanged();
            }
        }

        public VolumeControl CurrentVolume
        {
            get => _currentVolume;
            private set
            {
                _currentVolume = value;
                OnPropertyChanged();
            }
        }

    }
}
