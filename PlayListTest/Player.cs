using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayListTest
{
    public class Player : VmBase, IDisposable
    {
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan, TimeSpan> ProgressUpdated;

        public PlayStatus PlayStatus
        {
            get => _playStatus;
            private set
            {
                if (Equals(value, _playStatus)) return;
                _playStatus = value;
                PlayStatusChanged?.Invoke(value);
            }
        }

        public TimeSpan Duration { get; } = TimeSpan.FromMilliseconds(Rnd.Next(120000) + 120000);

        public TimeSpan PlayTime
        {
            get => _playTime;
            private set
            {
                if (Equals(_playTime, value)) return;
                _playTime = value;
                OnPropertyChanged();
            }
        }

        private readonly WiseStopwatch _sw = new WiseStopwatch();
        private PlayStatus _playStatus;
        private TimeSpan _playTime;

        private static readonly Random Rnd = new Random();

        public Player()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (_sw.Elapsed >= Duration && _sw.IsRunning)
                    {
                        PlayTime = Duration;
                        ProgressUpdated?.Invoke(PlayTime, Duration);
                        _sw.Stop();
                        if (PlayStatus == PlayStatus.Playing)
                            PlayStatus = PlayStatus.Finished;
                    }
                    else if (_sw.Elapsed < Duration)
                    {
                        PlayTime = _sw.Elapsed;
                        ProgressUpdated?.Invoke(PlayTime, Duration);
                    }

                    Thread.Sleep(50);
                }
            });
        }

        public void Play()
        {
            _sw.Start();
            PlayStatus = PlayStatus.Playing;
        }

        public void Pause()
        {
            _sw.Stop();
            PlayStatus = PlayStatus.Paused;
        }

        public void SkipTo(int milliseconds)
        {
            _sw.SkipTo(TimeSpan.FromMilliseconds(milliseconds));
            PlayStatus = PlayStatus.Paused;
        }

        public void Stop()
        {
            _sw.Reset();
            PlayStatus = PlayStatus.Paused;
        }

        public void Dispose()
        {
        }
    }
}