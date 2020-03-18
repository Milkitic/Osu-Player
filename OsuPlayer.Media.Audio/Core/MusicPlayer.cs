using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Core.SampleProviders;
using Milky.OsuPlayer.Media.Audio.Core.SoundTouch;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.Core
{
    internal sealed class MusicPlayer : Player, IDisposable
    {
        private static readonly string CachePath = Path.Combine(Domain.CachePath, "_temp.music");
        private static readonly object CacheLock = new object();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private PlayStatus _playStatus;

        private readonly object _propertiesLock = new object();
        private MyAudioFileReader _reader;

        private int _progressRefreshInterval;

        private readonly AudioPlaybackEngine _engine;
        private VarispeedSampleProvider _speedProvider;
        private readonly IWavePlayer _device;
        private string _filePath;
        private bool? _useTempo;
        private float _currentSpeed;

        private bool _soundTouchMode = false;

        #region Properties

        public override int ProgressRefreshInterval
        {
            get => _progressRefreshInterval;
            set
            {
                if (value < 10)
                    _progressRefreshInterval = 10;
                _progressRefreshInterval = value;
            }
        }

        public override PlayStatus PlayStatus
        {
            get => _playStatus;
            protected set
            {
                Console.WriteLine(@"Music: " + value);
                _playStatus = value;
            }
        }

        public override TimeSpan Duration
        {
            get => _reader.TotalTime;
            protected set => throw new InvalidOperationException();
        }

        public override TimeSpan PlayTime { get; protected set; }

        #endregion

        public MusicPlayer(AudioPlaybackEngine engine, IWavePlayer device, string filePath)
        {
            _engine = engine;
            _device = device;
            _filePath = filePath;
        }

        public override async Task InitializeAsync()
        {
            var fi = new FileInfo(_filePath);
            if (!fi.Exists)
            {
                _filePath = Path.Combine(Domain.DefaultPath, "blank.wav");
            }

            WaveResampler.Resample(_filePath, CachePath);
            _reader = new MyAudioFileReader(CachePath)
            {
                Volume = 1f * AppSettings.Default.Volume.Music * AppSettings.Default.Volume.Main
            };

            _speedProvider = new VarispeedSampleProvider(_reader, 10,
                new SoundTouchProfile(AppSettings.Default.Play.PlayUseTempo, false));
            var playbackRate = AppSettings.Default.Play.PlaybackRate;
            _engine.AddRootSample(_speedProvider);
            SetPlaybackRate(playbackRate);
            SetTime(TimeSpan.Zero, false);

            AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
            //AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
            var task = Task.Factory.StartNew(UpdateProgress, TaskCreationOptions.LongRunning);

            PlayStatus = PlayStatus.Ready;
            RaisePlayerLoadedEvent(this, new EventArgs());
            await Task.CompletedTask;
        }

        public override void Play()
        {
            PlayWithoutNotify();

            PlayStatus = PlayStatus.Playing;
            RaisePlayerStartedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Pause()
        {
            PauseWithoutNotify();

            PlayStatus = PlayStatus.Paused;
            RaisePlayerPausedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Replay()
        {
            SetTime(TimeSpan.Zero);
            Play();
        }

        public override void SetTime(TimeSpan time, bool play = true)
        {
            if (time < TimeSpan.Zero) time = TimeSpan.Zero;
            if (_reader != null)
            {
                _reader.CurrentTime =
                    time >= _reader.TotalTime ? _reader.TotalTime - TimeSpan.FromMilliseconds(1) : time;
                _speedProvider.Reposition();
            }

            if (!play) PauseWithoutNotify();
        }

        public override void Stop()
        {
            ResetWithoutNotify();
            RaisePlayerStoppedEvent(this, new EventArgs());
        }

        internal void SetTempoMode(bool useTempo)
        {
            if (useTempo == _useTempo) return;

            _useTempo = useTempo;
            _speedProvider.SetSoundTouchProfile(new SoundTouchProfile(useTempo, false));
        }

        internal void SetPlaybackRate(float speed)
        {
            _currentSpeed = speed;
            _speedProvider.PlaybackRate = speed;
        }

        private void PauseWithoutNotify()
        {
            _device?.Pause();
        }

        private void PlayWithoutNotify()
        {
            _device.Play();
        }

        private void UpdateProgress()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_reader != null && PlayStatus != PlayStatus.NotInitialized && PlayStatus != PlayStatus.Finished)
                {
                    if (_reader.CurrentTime < _reader.TotalTime)
                    {
                        PlayTime = _reader.CurrentTime;
                    }
                    else
                    {
                        PlayStatus = PlayStatus.Finished;
                        RaisePlayerFinishedEvent(this, new EventArgs());
                    }
                }

                Thread.Sleep(5);
            }
        }

        private void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _reader.Volume = 1f * AppSettings.Default.Volume.Music * AppSettings.Default.Volume.Main;
        }

        internal void ResetWithoutNotify()
        {
            SetTime(TimeSpan.Zero, false);
            PlayStatus = PlayStatus.Stopped;
        }

        public override void Dispose()
        {
            base.Dispose();

            _cts.Cancel();
            _reader?.Dispose();
            _reader = null;
            _speedProvider?.Dispose();
            _speedProvider = null;
            _cts?.Dispose();

            AppSettings.Default.Volume.PropertyChanged -= Volume_PropertyChanged;
        }
    }
}
