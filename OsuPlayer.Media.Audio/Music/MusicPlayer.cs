using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Music.SampleProviders;
using Milky.OsuPlayer.Media.Audio.Music.SoundTouch;
using Milky.OsuPlayer.Media.Audio.Music.WaveProviders;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Music
{
    internal sealed class MusicPlayer : Player, IDisposable
    {
        private const int Latency = 5;
        private static bool UseSoundTouch => AppSettings.Current.Play.UsePlayerV2;
        private static bool WaitingMode => true;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private PlayerStatus _playerStatus;

        private readonly object _propertiesLock = new object();
        private IWavePlayer _device;
        private MyAudioFileReader _reader;

        private int _progressRefreshInterval;

        private string _filePath;

        public MusicPlayer(string filePath)
        {
            _filePath = filePath;
        }

        public override async Task InitializeAsync()
        {
            var fi = new FileInfo(_filePath);
            if (!fi.Exists)
            {
                _filePath = Path.Combine(Domain.DefaultPath, "blank.wav");
            }

            _reader = new MyAudioFileReader(_filePath)
            {
                Volume = 1f * AppSettings.Current.Volume.Music * AppSettings.Current.Volume.Main
            };

            _device = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, Latency);
            _device.PlaybackStopped += (sender, args) =>
            {
                PlayerStatus = PlayerStatus.Finished;
                RaisePlayerFinishedEvent(this, new EventArgs());
            };

            _device.Init(_reader);

            AppSettings.Current.Volume.PropertyChanged += Volume_PropertyChanged;
            Task.Factory.StartNew(UpdateProgress, TaskCreationOptions.LongRunning);

            PlayerStatus = PlayerStatus.Ready;
            RaisePlayerLoadedEvent(this, new EventArgs());
            await Task.CompletedTask;
        }

        private void UpdateProgress()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_reader != null && PlayerStatus != PlayerStatus.NotInitialized && PlayerStatus != PlayerStatus.Finished)
                {
                    PlayTime = (int)_reader?.CurrentTime.TotalMilliseconds;
                }

                Thread.Sleep(5);
            }
        }

        private void Volume_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _reader.Volume = 1f * AppSettings.Current.Volume.Music * AppSettings.Current.Volume.Main;
        }

        public override void Play()
        {
            PlayWithoutNotify();

            PlayerStatus = PlayerStatus.Playing;
            RaisePlayerStartedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        private void PlayWithoutNotify()
        {
            _device.Play();
        }

        public override void Pause()
        {
            PauseWithoutNotify();

            PlayerStatus = PlayerStatus.Paused;
            RaisePlayerPausedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        private void PauseWithoutNotify()
        {
            _device?.Pause();
        }

        public override void Replay()
        {
            SetTime(0);
            Play();
        }

        public override void SetTime(int ms, bool play = true)
        {
            if (ms < 0) ms = 0;
            var span = new TimeSpan(0, 0, 0, 0, ms);
            if (_reader != null)
            {
                _reader.CurrentTime = span >= _reader.TotalTime ? _reader.TotalTime - new TimeSpan(0, 0, 0, 0, 1) : span;
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

        internal void ResetWithoutNotify()
        {
            SetTime(0, false);
            PlayerStatus = PlayerStatus.Stopped;
        }

        public override void Dispose()
        {
            base.Dispose();

            _cts.Cancel();
            _device?.Dispose();
            _device = null;
            _reader?.Dispose();
            _reader = null;
            _cts?.Dispose();

            AppSettings.Current.Volume.PropertyChanged -= Volume_PropertyChanged;
        }
        
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
    }
}
