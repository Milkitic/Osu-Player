using Milky.OsuPlayer.Shared;
using System;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Player
{
    public abstract class Subchannel : IChannel
    {
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan> PositionUpdated;
        public virtual float Volume { get => SampleControl.Volume; set => SampleControl.Volume = value; }

        private PlayStatus _playStatus;
        private TimeSpan _position;
        protected AudioPlaybackEngine Engine { get; }

        public SampleControl SampleControl { get; } = new SampleControl();

        public Subchannel(AudioPlaybackEngine engine)
        {
            Engine = engine;
        }

        public abstract TimeSpan ChannelStartTime { get; }
        public TimeSpan ChannelEndTime => ChannelStartTime + Duration;

        public string Description { get; set; }

        public abstract TimeSpan Duration { get; protected set; }

        public virtual TimeSpan Position
        {
            get => _position;
            protected set
            {
                if (value == _position) return;
                _position = value;
                RaisePositionUpdated(value);
            }
        }

        protected void RaisePositionUpdated(TimeSpan value)
        {
            InvokeMethodHelper.OnMainThread(() => PositionUpdated?.Invoke(value));
        }

        public abstract float PlaybackRate { get; protected set; }
        public abstract bool UseTempo { get; protected set; }

        public PlayStatus PlayStatus
        {
            get => _playStatus;
            protected set
            {
                if (value == _playStatus) return;
                _playStatus = value;
                InvokeMethodHelper.OnMainThread(() => PlayStatusChanged?.Invoke(value));
            }
        }

        public abstract Task Initialize();

        public abstract Task Play();

        public abstract Task Pause();

        public abstract Task Stop();

        public abstract Task Restart();

        public abstract Task SkipTo(TimeSpan time);

        public abstract Task Sync(TimeSpan time);

        public abstract Task SetPlaybackRate(float rate, bool useTempo);

        public virtual async Task DisposeAsync()
        {
            Engine?.Dispose();
            await Task.CompletedTask;
        }
    }
}