using System.ComponentModel;
using System.Runtime.CompilerServices;
using Milky.OsuPlayer.Common.Annotations;
using Milky.OsuPlayer.Common.Player;
using Newtonsoft.Json;
using OsuPlayer.Devices;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class PlayControl : INotifyPropertyChanged
    {
        private bool _playUseTempo;
        private float _playbackRate = 1;

        public int GeneralOffset { get; set; }
        [JsonIgnore]
        public int GeneralActualOffset => GeneralOffset + 0;
        public bool ReplacePlayList { get; set; } = true;
        public bool UsePlayerV2 { get; set; } = false;

        public float PlaybackRate
        {
            get => _playbackRate;
            set
            {
                if (value.Equals(_playbackRate)) return;
                _playbackRate = value;
                OnPropertyChanged();
            }
        }

        public bool PlayUseTempo
        {
            get => _playUseTempo;
            set
            {
                if (value == _playUseTempo) return;
                _playUseTempo = value;
                OnPropertyChanged();
            }
        }

        public bool AutoPlay { get; set; } = false;
        public bool Memory { get; set; } = true;
        public IDeviceInfo DeviceInfo { get; set; } = null;
        public int DesiredLatency { get; set; } = 5;
        public bool IsExclusive { get; set; } = false;
        public PlayMode PlayMode { get; set; } = PlayMode.Normal;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}