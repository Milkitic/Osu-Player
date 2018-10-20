using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Milkitic.OsuPlayer.Media.Music
{
    public class MusicPlayer : IPlayer, IDisposable
    {
        public PlayerStatus PlayerStatus
        {
            get => _playerStatus;
            private set
            {
                Console.WriteLine(@"Music: " + value);
                _playerStatus = value;
            }
        }

        public int Duration => (int)_audioFile.TotalTime.TotalMilliseconds;
        public int PlayTime { get; private set; }

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private WaveOutEvent _device;
        private MyAudioFileReader _audioFile;
        private PlayerStatus _playerStatus;

        public MusicPlayer(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists)
                throw new FileNotFoundException("找不到音乐文件…", fi.FullName);
            _device = new WaveOutEvent { DesiredLatency = App.Config.Play.DesiredLatency };
            _device.PlaybackStopped += (sender, args) =>
            {
                PlayerStatus = PlayerStatus.Finished;
            };

            _audioFile = new MyAudioFileReader(filePath);
            _device.Init(_audioFile);

            PlayerStatus = PlayerStatus.Ready;

            Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (PlayerStatus != PlayerStatus.NotInitialized && _audioFile != null)
                    {
                        _audioFile.Volume = App.Config.Volume.Main * App.Config.Volume.Music;
                        PlayTime = (int)_audioFile?.CurrentTime.TotalMilliseconds;
                    }
                    Thread.Sleep(10);
                }
            });
        }

        public void Play()
        {
            _device.Play();
            PlayerStatus = PlayerStatus.Playing;
        }

        public void Pause()
        {
            _device.Pause();
            PlayerStatus = PlayerStatus.Paused;
        }

        public void Replay()
        {
            SetTime(0);
            Play();
        }

        public void SetTime(int ms, bool play = true)
        {
            var span = new TimeSpan(0, 0, 0, 0, ms);
            _audioFile.CurrentTime = span >= _audioFile.TotalTime ? _audioFile.TotalTime - new TimeSpan(0, 0, 0, 0, 1) : span;
            PlayerStatus = PlayerStatus.Playing;
            if (!play) Pause();
        }

        public void Stop()
        {
            SetTime(0, false);
            PlayerStatus = PlayerStatus.Stopped;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _device?.Dispose();
            _device = null;
            _audioFile?.Dispose();
            _audioFile = null;
            _cts?.Dispose();
        }
    }
}
