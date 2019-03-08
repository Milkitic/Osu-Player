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

    internal class PlayerViewModel : ViewModelBase
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
        }

        private bool _isPlaying;
        private bool _enableVideo;
        private PlayListMode _playListMode;

        private long _position;
        private long _duration;
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
    }
}
