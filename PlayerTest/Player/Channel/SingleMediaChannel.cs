using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PlayerTest.SoundTouch;
using PlayerTest.Wave;

namespace PlayerTest.Player.Channel
{
    public class SingleMediaChannel : Subchannel
    {
        private readonly string _path;
        private readonly float _playbackRate;
        private readonly bool _useTempo;

        private MyAudioFileReader _fileReader;
        private VarispeedSampleProvider _speedProvider;
        private ISampleProvider _actualRoot;

        public override TimeSpan Duration { get; protected set; }
        public override TimeSpan Position { get; protected set; }
        public override float PlaybackRate { get; protected set; }
        public override bool UseTempo { get; protected set; }

        public SingleMediaChannel(AudioPlaybackEngine engine, string path, float playbackRate, bool useTempo) : base(engine)
        {
            _path = path;
            _playbackRate = playbackRate;
            _useTempo = useTempo;
        }

        public override async Task Initialize()
        {
            _speedProvider = new VarispeedSampleProvider(_fileReader,
                10,
                new SoundTouchProfile(_useTempo, false)
            )
            {
                PlaybackRate = _playbackRate
            };

            var stream = await WaveFormatFactory.Resample(_path);
            _fileReader = new MyAudioFileReader(stream, StreamType.Wav);

            Duration = _fileReader.TotalTime;

            SampleControl.Volume = 1;
            SampleControl.Balance = 0;
            PlayStatus = ChannelStatus.Ready;
            await Task.CompletedTask;
        }

        public override async Task Play()
        {
            if (!Engine.RootMixer.MixerInputs.Contains(_speedProvider))
                Engine.RootMixer.AddMixerInput(_speedProvider, SampleControl, out _actualRoot);
            PlayStatus = ChannelStatus.Playing;
            await Task.CompletedTask;
        }

        public override async Task Pause()
        {
            Engine.RootMixer.RemoveMixerInput(_actualRoot);
            PlayStatus = ChannelStatus.Paused;
            await Task.CompletedTask;
        }

        public override async Task Stop()
        {
            Engine.RootMixer.RemoveMixerInput(_actualRoot);
            await SetTime(TimeSpan.Zero);
            PlayStatus = ChannelStatus.Paused;
            await Task.CompletedTask;
        }

        public override async Task Restart()
        {
            await SetTime(TimeSpan.Zero);
            await Play();
            await Task.CompletedTask;
        }

        public override async Task SetTime(TimeSpan time)
        {
            _fileReader.CurrentTime = time >= _fileReader.TotalTime
                ? _fileReader.TotalTime - TimeSpan.FromMilliseconds(1)
                : time;
            _speedProvider.Reposition();
            await Task.CompletedTask;
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            if (!PlaybackRate.Equals(rate))
            {
                PlaybackRate = rate;
                _speedProvider.PlaybackRate = rate;
            }

            if (UseTempo != useTempo)
            {
                _speedProvider.SetSoundTouchProfile(new SoundTouchProfile(_useTempo, false));
            }

            await Task.CompletedTask;
        }
    }
}
