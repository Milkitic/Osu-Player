//using Milky.OsuPlayer.Core.Configuration;
//using Milky.OsuPlayer.Core.Player;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Input;
//using Milky.OsuPlayer.Core;
//using Milky.OsuPlayer.Media.Audio;

//namespace Milky.OsuPlayer.ViewModels
//{
//    public class PlayerViewModel : VmBase
//    {
//        public static PlayerViewModel Current { get; set; }

//        public static void InitViewModel()
//        {
//            if (Current != null)
//                return;
//            Current = new PlayerViewModel();
//        }

//        private PlayerViewModel()
//        {
//            CurrentVolume = AppSettings.Default.Volume;
//        }

//        private bool _isPlaying;
//        private bool _enableVideo = true;

//        private VolumeControl _currentVolume;

//        public bool IsPlaying
//        {
//            get => _isPlaying;
//            set
//            {
//                _isPlaying = value;
//                OnPropertyChanged();
//            }
//        }

//        public bool EnableVideo
//        {
//            get => _enableVideo;
//            set
//            {
//                _enableVideo = value;
//                OnPropertyChanged();
//            }
//        }

//        public ObservablePlayController Controller { get; } = Services.Get<ObservablePlayController>();

//        public VolumeControl CurrentVolume
//        {
//            get => _currentVolume;
//            private set
//            {
//                _currentVolume = value;
//                OnPropertyChanged();
//            }
//        }

//    }
//}
