using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NAudio.Wave;
using PlayerTest.Annotations;

namespace PlayerTest.Player
{
    public abstract class Subchannel : IChannel, INotifyPropertyChanged
    {
        public event Action<ChannelStatus> PlayStatusChanged;

        private ChannelStatus _playStatus;
        protected AudioPlaybackEngine Engine { get; }

        public Subchannel(AudioPlaybackEngine engine)
        {
            Engine = engine;
        }

        public string Description { get; set; }

        public abstract TimeSpan Duration { get; protected set; }
        public abstract TimeSpan Position { get; protected set; }
        public abstract float PlaybackRate { get; protected set; }
        public abstract bool UseTempo { get; protected set; }

        public ChannelStatus PlayStatus
        {
            get => _playStatus;
            protected set
            {
                if (value == _playStatus) return;
                _playStatus = value;
                PlayStatusChanged?.Invoke(value);
            }
        }

        public abstract Task Initialize();

        public abstract Task Play();

        public abstract Task Pause();

        public abstract Task Stop();

        public abstract Task Restart();

        public abstract Task SetTime(TimeSpan time);

        public abstract Task SetPlaybackRate(float rate, bool useTempo);
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}