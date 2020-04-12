using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio.SoundTouch;
using Milky.OsuPlayer.Media.Audio.Wave;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Player.Subchannels
{
    public class SingleMediaChannel : Subchannel
    {
        private readonly string _path;

        private MyAudioFileReader _fileReader;
        private VariableSpeedSampleProvider _speedProvider;
        private ISampleProvider _actualRoot;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly VariableStopwatch _sw = new VariableStopwatch();
        private ConcurrentQueue<double> _offsetQueue = new ConcurrentQueue<double>();
        private int? _referenceOffset;
        private Task _backoffTask;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public override float Volume
        {
            get => _fileReader?.Volume ?? 0;
            set { if (_fileReader != null) _fileReader.Volume = value; }
        }

        public override TimeSpan Duration { get; protected set; }

        public override TimeSpan ChannelStartTime => TimeSpan.FromMilliseconds(AppSettings.Default.Play.GeneralActualOffset < 0
            ? -AppSettings.Default.Play.GeneralActualOffset
            : 0);

        public sealed override float PlaybackRate { get; protected set; }
        public sealed override bool UseTempo { get; protected set; }

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

            //await CachedSound.CreateCacheSounds(new[] { _path }).ConfigureAwait(false);

            Duration = _fileReader.TotalTime;

            SampleControl.Volume = 1;
            SampleControl.Balance = 0;
            PlayStatus = PlayStatus.Ready;
            _backoffTask = new Task(() =>
            {
                const int avgCount = 30;

                var oldTime = TimeSpan.Zero;
                var stdOffset = 0;
                while (!_cts.IsCancellationRequested)
                {
                    Position = _sw.Elapsed /*newTime*/ - TimeSpan.FromMilliseconds(_referenceOffset ?? 0);

                    var newTime = _fileReader.CurrentTime;
                    if (oldTime != newTime)
                    {
                        oldTime = newTime;
                        var offset = _sw.Elapsed - _fileReader.CurrentTime;
                        if (_offsetQueue.Count < avgCount)
                        {
                            _offsetQueue.Enqueue(offset.TotalMilliseconds);
                            if (_offsetQueue.Count == avgCount)
                            {
                                var avg = (int)_offsetQueue.Average();
                                stdOffset = avg;
                                Logger.Debug("{0}: avg offset: {1}", Description, avg);
                            }
                        }
                        else
                        {
                            if (_offsetQueue.TryDequeue(out _))
                            {
                                _offsetQueue.Enqueue(offset.TotalMilliseconds);
                                var refOffset = (int)_offsetQueue.Average() - stdOffset;
                                if (refOffset != _referenceOffset)
                                {
                                    _referenceOffset = refOffset;
                                    //Logger.Debug("{0}: {1}: {2}", Description, nameof(_referenceOffset),
                                    //    _referenceOffset);
                                }
                            }
                        }
                    }

                    Thread.Sleep(1);
                }
            }, TaskCreationOptions.LongRunning);
            _backoffTask.Start();
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
            Position = time/*_fileReader.CurrentTime*/;
            Logger.Debug("{0} skip: want: {1}; actual: {2}", Description, time, Position);
            _sw.SkipTo(time);

            _referenceOffset = null;
            _offsetQueue = new ConcurrentQueue<double>();
            PlayStatus = status;
            await Task.CompletedTask;
        }

        public override async Task Sync(TimeSpan time)
        {
            _fileReader.CurrentTime = time >= _fileReader.TotalTime
                ? _fileReader.TotalTime - TimeSpan.FromMilliseconds(1)
                : time;
            _speedProvider.Reposition();
            Position = time/*_fileReader.CurrentTime*/;
            _sw.SkipTo(time);
            await Task.CompletedTask;
        }

        public override async Task SetPlaybackRate(float rate, bool useTempo)
        {
            bool changed = !rate.Equals(PlaybackRate) || useTempo != UseTempo;
            if (!PlaybackRate.Equals(rate))
            {
                PlaybackRate = rate;
                _speedProvider.PlaybackRate = rate;
                _sw.Rate = rate;
            }

            if (UseTempo != useTempo)
            {
                _speedProvider.SetSoundTouchProfile(new SoundTouchProfile(useTempo, false));
                UseTempo = useTempo;
            }

            if (changed) await SkipTo(_sw.Elapsed);
            await Task.CompletedTask;
        }

        public override async Task DisposeAsync()
        {
            _cts?.Cancel();
            Logger.Debug($"Disposing: Canceled {nameof(_cts)}.");
            await Task.WhenAll(_backoffTask);
            Logger.Debug($"Disposing: Stopped task {nameof(_backoffTask)}.");
            _cts?.Dispose();
            Logger.Debug($"Disposing: Disposed {nameof(_cts)}.");
            await Stop();
            Logger.Debug($"Disposing: Stopped.");
            await base.DisposeAsync();
            Logger.Debug($"Disposing: Disposed base.");
            _speedProvider?.Dispose();
            Logger.Debug($"Disposing: Disposed {nameof(_speedProvider)}.");
            _fileReader?.Dispose();
            Logger.Debug($"Disposing: Disposed {nameof(_fileReader)}.");
        }
    }
}