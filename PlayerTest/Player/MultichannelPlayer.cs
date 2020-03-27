using NAudio.Wave;
using PlayerTest.Device;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTest.Player.Channel;

namespace PlayerTest.Player
{
    public abstract class MultichannelPlayer : IChannel
    {
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan> PositionUpdated;

        public string Description { get; } = nameof(MultichannelPlayer);

        public TimeSpan Duration { get; private set; }
        public TimeSpan Position { get; private set; }

        public float PlaybackRate { get; private set; }
        public bool UseTempo { get; private set; }

        public PlayStatus PlayStatus { get; private set; }
        public StopMode StopMode { get; set; }

        public float Volume
        {
            get => Engine.RootVolume;
            set => Engine.RootVolume = value;
        }

        private readonly List<IChannel> _subchannels = new List<IChannel>();

        private readonly IWavePlayer _outputDevice;
        protected readonly AudioPlaybackEngine Engine;

        public MultichannelPlayer()
        {
            _outputDevice = DeviceProvider.CreateOrGetDefaultDevice();
            Engine = new AudioPlaybackEngine(_outputDevice);
        }

        public abstract Task Initialize();

        protected void AddSubchannel(IChannel channel)
        {
            _subchannels.Add(channel);
        }

        protected void RemoveSubchannel(IChannel channel)
        {
            _subchannels.Remove(channel);
        }

        protected IEnumerable<IChannel> EnumerateSubchannels()
        {
            foreach (var subchannel in _subchannels)
            {
                yield return subchannel;
            }
        }

        public async Task Play()
        {
            foreach (var channel in _subchannels)
            {
                await channel.Play();
            }
        }

        public async Task Pause()
        {
            foreach (var channel in _subchannels)
            {
                await channel.Pause();
            }
        }

        public async Task Stop()
        {
            foreach (var channel in _subchannels)
            {
                await channel.Stop();
            }
        }

        public async Task Restart()
        {
            foreach (var channel in _subchannels)
            {
                await channel.Restart();
            }
        }

        public async Task SkipTo(TimeSpan time)
        {
            foreach (var channel in _subchannels)
            {
                await channel.SkipTo(time);
            }
        }

        public async Task SetPlaybackRate(float rate, bool useTempo)
        {
            foreach (var channel in _subchannels)
            {
                await channel.SetPlaybackRate(rate, useTempo);
            }

            PlaybackRate = rate;
            UseTempo = useTempo;
        }
    }
}