using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Music;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio
{
    public abstract class Player : IPlayer
    {
        public event EventHandler PlayerLoaded;
        public event EventHandler<ProgressEventArgs> PlayerStarted;
        public event EventHandler PlayerStopped;
        public event EventHandler<ProgressEventArgs> PlayerPaused;
        public event EventHandler PlayerFinished;
        public event EventHandler<ProgressEventArgs> PositionChanged;
        public event EventHandler<ProgressEventArgs> PositionSet;

        protected virtual void RaisePlayerLoadedEvent(object sender, EventArgs e) => PlayerLoaded?.Invoke(sender, e);
        protected virtual void RaisePlayerStartedEvent(object sender, ProgressEventArgs e) => PlayerStarted?.Invoke(sender, e);
        protected virtual void RaisePlayerStoppedEvent(object sender, EventArgs e) => PlayerStopped?.Invoke(sender, e);
        protected virtual void RaisePlayerPausedEvent(object sender, ProgressEventArgs e) => PlayerPaused?.Invoke(sender, e);
        protected virtual void RaisePlayerFinishedEvent(object sender, EventArgs e) => PlayerFinished?.Invoke(sender, e);
        protected virtual void RaiseProgressChangedEvent(object sender, ProgressEventArgs e) => PositionChanged?.Invoke(sender, e);
        protected virtual void RaiseProgressSetEvent(object sender, ProgressEventArgs e) => PositionChanged?.Invoke(sender, e);

        protected virtual void NotifyProgress(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
            {
                var oldT = PlayTime;
                var sw = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(10);
                    var newT = PlayTime;
                    if (newT != oldT &&
                        sw.ElapsedMilliseconds > ProgressRefreshInterval - 10 &&
                        PlayerStatus == PlayerStatus.Playing)
                    {
                        oldT = newT;
                        PositionChanged?.Invoke(this, new ProgressEventArgs(PlayTime, Duration));
                        sw.Restart();
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }


        public abstract int ProgressRefreshInterval { get; set; }

        public abstract PlayerStatus PlayerStatus { get; protected set; }
        public abstract int Duration { get; protected set; }
        public abstract int PlayTime { get; protected set; }

        public abstract void Play();
        public abstract void Pause();
        public abstract void Stop();
        public abstract void Replay();
        public abstract void SetTime(int ms, bool play = true);
    }
}