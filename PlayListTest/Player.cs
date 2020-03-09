using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlayListTest
{
    public class Player
    {
        public event Action<PlayStatus> PlayStatusChanged;

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

        public double Duration { get; } = Rnd.Next(120000) + 120000;
        public double PlayTime { get; private set; }

        private readonly WiseStopwatch _sw = new WiseStopwatch();
        private PlayStatus _playStatus;

        private static readonly Random Rnd = new Random();

        public Player()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (_sw.ElapsedMilliseconds >= Duration && _sw.IsRunning)
                    {
                        PlayTime = Duration;
                        _sw.Stop();
                        PlayStatus = PlayStatus.Finished;
                    }
                    else if (_sw.ElapsedMilliseconds < Duration)
                    {
                        PlayTime = _sw.ElapsedMilliseconds;
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
            _sw.Stop();
            PlayStatus = PlayStatus.Paused;
        }

        public void Stop()
        {
            _sw.Reset();
            PlayStatus = PlayStatus.Paused;
        }

        public void Restart()
        {
            _sw.Restart();
        }
    }
}