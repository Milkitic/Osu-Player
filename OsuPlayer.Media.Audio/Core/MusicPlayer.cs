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
    public sealed class MusicPlayer : Player, IDisposable
    {
        private static readonly string CachePath = Path.Combine(Domain.CachePath, "_temp.music");
        private static readonly object CacheLock = new object();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private PlayerStatus _playerStatus;

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

        public override PlayerStatus PlayerStatus
        {
            get => _playerStatus;
            protected set
            {
                Console.WriteLine(@"Music: " + value);
                _playerStatus = value;
            }
        }

        public override int Duration
        {
            get => (int)_reader.TotalTime.TotalMilliseconds;
            protected set => throw new InvalidOperationException();
        }

        public override int PlayTime { get; protected set; }

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


            //_device.PlaybackStopped += (sender, args) =>
            //{
            //    PlayerStatus = PlayerStatus.Finished;
            //    RaisePlayerFinishedEvent(this, new EventArgs());
            //};
            _speedProvider = new VarispeedSampleProvider(_reader, 10,
                new SoundTouchProfile(AppSettings.Default.Play.PlayUseTempo, false));
            var playbackRate = AppSettings.Default.Play.PlaybackRate;
            _engine.AddRootSample(_speedProvider);
            SetPlaybackRate(playbackRate);
            //if (Math.Round(playbackRate, 3) - 1 < 0.001)
            //{
            //    _engine.AddRootSample(_reader);
            //    _soundTouchMode = false;
            //}
            //else
            //{
            //    _engine.AddRootSample(_speedProvider);
            //    _soundTouchMode = true;
            //}

            SetTime(0, false);

            AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
            //AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;
            var task = Task.Factory.StartNew(UpdateProgress, TaskCreationOptions.LongRunning);

            PlayerStatus = PlayerStatus.Ready;
            RaisePlayerLoadedEvent(this, new EventArgs());
            await Task.CompletedTask;
        }

        public override void Play()
        {
            PlayWithoutNotify();

            PlayerStatus = PlayerStatus.Playing;
            RaisePlayerStartedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Pause()
        {
            PauseWithoutNotify();

            PlayerStatus = PlayerStatus.Paused;
            RaisePlayerPausedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Replay()
        {
            SetTime(0);
            Play();
        }

        public override void SetTime(int ms, bool play = true)
        {
            if (ms < 0) ms = 0;
            var span = TimeSpan.FromMilliseconds(ms);
            if (_reader != null)
            {
                _reader.CurrentTime =
                    span >= _reader.TotalTime ? _reader.TotalTime - TimeSpan.FromMilliseconds(1) : span;
                _speedProvider.Reposition();
            }
            //PlayerStatus = PlayerStatus.Playing;
            if (!play) PauseWithoutNotify();
            //else PlayWithoutNotify();
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
            //if (Math.Abs(speed - 1) < 0.001)
            //{
            //    _engine.RemoveRootSample(_speedProvider);
            //    _engine.AddRootSample(_reader);
            //    _soundTouchMode = false;
            //}
            //else
            //{
            //    _engine.RemoveRootSample(_reader);
            //    _engine.AddRootSample(_speedProvider);
            //    _soundTouchMode = true;
            _currentSpeed = speed;
            _speedProvider.PlaybackRate = speed;
            //}
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
                if (_reader != null && PlayerStatus != PlayerStatus.NotInitialized && PlayerStatus != PlayerStatus.Finished)
                {
                    if (_reader.CurrentTime < _reader.TotalTime)
                    {
                        PlayTime = (int)_reader.CurrentTime.TotalMilliseconds;
                    }
                    else
                    {
                        PlayerStatus = PlayerStatus.Finished;
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

        //private void Play_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case nameof(AppSettings.Play.PlayUseTempo):
        //            SetTempoMode(AppSettings.Default.Play.PlayUseTempo);
        //            break;
        //        case nameof(AppSettings.Play.PlaybackRate):
        //            SetPlaybackRate(AppSettings.Default.Play.PlaybackRate);
        //            break;
        //    }
        //}

        internal void ResetWithoutNotify()
        {
            SetTime(0, false);
            PlayerStatus = PlayerStatus.Stopped;
        }

        public override void Dispose()
        {
            base.Dispose();

            _cts.Cancel();
            //_device?.Dispose();
            //_device = null;
            _reader?.Dispose();
            _reader = null;
            _speedProvider?.Dispose();
            _speedProvider = null;
            _cts?.Dispose();

            AppSettings.Default.Volume.PropertyChanged -= Volume_PropertyChanged;
            //AppSettings.Default.Play.PropertyChanged -= Play_PropertyChanged;
        }
    }
}
