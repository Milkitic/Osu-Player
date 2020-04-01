using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio.SoundTouch;
using Milky.OsuPlayer.Media.Audio.Wave;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.Player.Subchannels
{
    public class SingleMediaChannel : Subchannel
    {
        private readonly string _path;

        private MyAudioFileReader _fileReader;
        private VariableSpeedSampleProvider _speedProvider;
        private ISampleProvider _actualRoot;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public override float Volume
        {
            get => _fileReader?.Volume ?? 0;
            set { if (_fileReader != null) _fileReader.Volume = value; }
        }

        public override TimeSpan Duration { get; protected set; }

        public override TimeSpan ChannelStartTime => TimeSpan.FromMilliseconds(AppSettings.Default.Play.GeneralActualOffset < 0
            ? -AppSettings.Default.Play.GeneralActualOffset
            : 0);

        public TimeSpan ReferenceDuration =>
            Duration.Add(TimeSpan.FromMilliseconds(AppSettings.Default.Play.GeneralActualOffset));

        public TimeSpan ReferencePosition =>
            Position.Add(TimeSpan.FromMilliseconds(AppSettings.Default.Play.GeneralActualOffset));

        public sealed override float PlaybackRate { get; protected set; }
        public sealed override bool UseTempo { get; protected set; }

        private VariableStopwatch _sw = new VariableStopwatch();

        public SingleMediaChannel(AudioPlaybackEngine engine, string path, float playbackRate, bool useTempo) :
            base(engine)
        {
            _path = path;
            PlaybackRate = playbackRate;
            UseTempo = useTempo;
        }

        public override async Task Initialize()
        {
            _fileReader = await WaveFormatFactory.GetResampledAudioFileReader(_path, CachedSound.WaveStreamType)
                .ConfigureAwait(false);

            _speedProvider = new VariableSpeedSampleProvider(_fileReader,
                10,
                new SoundTouchProfile(UseTempo, false)
            )
            {
                PlaybackRate = PlaybackRate
            };

            await CachedSound.CreateCacheSounds(new[] { _path }).ConfigureAwait(false);

            Duration = _fileReader.TotalTime;

            SampleControl.Volume = 1;
            SampleControl.Balance = 0;
            PlayStatus = PlayStatus.Ready;
            new Task(() =>
            {
                var oldTime = TimeSpan.Zero;
                while (!_cts.IsCancellationRequested)
                {
                    var newTime = _fileReader.CurrentTime;
                    if (oldTime != newTime)
                    {
                        Position = newTime;
                        oldTime = newTime;
                        Console.WriteLine(_sw.Elapsed - _fileReader.CurrentTime);
                    }

                    Thread.Sleep(5);
                }
            }, TaskCreationOptions.LongRunning).Start();
            await Task.CompletedTask;
        }

        public override async Task Play()
        {
            if (!Engine.RootMixer.MixerInputs.Contains(_actualRoot))
                Engine.RootMixer.AddMixerInput(_speedProvider, SampleControl, out _actualRoot);
            PlayStatus = PlayStatus.Playing;
            _sw.Start();
            await Task.CompletedTask;
        }

        public override async Task Pause()
        {
            Engine.RootMixer.RemoveMixerInput(_actualRoot);
            PlayStatus = PlayStatus.Paused;
            _sw.Stop();
            await Task.CompletedTask;
        }

        public override async Task Stop()
        {
            Engine.RootMixer.RemoveMixerInput(_actualRoot);
            await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
            PlayStatus = PlayStatus.Paused;
            _sw.Reset();
            await Task.CompletedTask;
        }

        public override async Task Restart()
        {
            await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
            await Play().ConfigureAwait(false);
            _sw.Restart();
            await Task.CompletedTask;
        }

        public override async Task SkipTo(TimeSpan time)
        {
            var status = PlayStatus;
            PlayStatus = PlayStatus.Reposition;
            if (_fileReader.TotalTime > TimeSpan.Zero)
                _fileReader.CurrentTime = time >= _fileReader.TotalTime
                    ? _fileReader.TotalTime - TimeSpan.FromMilliseconds(1)
                    : time;
            _speedProvider.Reposition();
            Position = _fileReader.CurrentTime;
            Console.WriteLine($"{Description} skip: want: {time}; actual: {Position}");
            _sw.SkipTo(time);

            PlayStatus = status;
            await Task.CompletedTask;
        }

        public override async Task Sync(TimeSpan time)
        {
            _fileReader.CurrentTime = time >= _fileReader.TotalTime
                ? _fileReader.TotalTime - TimeSpan.FromMilliseconds(1)
                : time;
            _speedProvider.Reposition();
            Position = _fileReader.CurrentTime;
            _sw.SkipTo(time);
            await Task.CompletedTask;
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            if (!PlaybackRate.Equals(rate))
            {
                PlaybackRate = rate;
                _speedProvider.PlaybackRate = rate;
                _sw.Rate = rate;
            }

            if (UseTempo != useTempo)
            {
                _speedProvider.SetSoundTouchProfile(new SoundTouchProfile(UseTempo, false));
                UseTempo = useTempo;
            }

            await Task.CompletedTask;
        }

        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();
            await Stop();
            _speedProvider?.Dispose();
            _fileReader?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}