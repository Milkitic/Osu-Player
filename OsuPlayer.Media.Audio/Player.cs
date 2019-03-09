using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Music;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio
{
    public abstract class Player : IPlayer
    {

        private static readonly SynchronizationContext UiContext;

        static Player()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                var fileName = Path.GetFileName(assembly.Location);
                if (fileName == "System.Windows.Forms.dll")
                {
                    var type = assembly.DefinedTypes.First(k => k.Name.StartsWith("WindowsFormsSynchronizationContext"));
                    UiContext = (SynchronizationContext)Activator.CreateInstance(type);
                    break;
                }
                else if (fileName == "WindowsBase.dll")
                {
                    var type = assembly.DefinedTypes.First(k => k.Name.StartsWith("DispatcherSynchronizationContext"));
                    UiContext = (SynchronizationContext)Activator.CreateInstance(type);
                    break;
                }
            }

            if (UiContext == null) UiContext = SynchronizationContext.Current;
        }

        public event EventHandler PlayerLoaded;
        public event EventHandler<ProgressEventArgs> PlayerStarted;
        public event EventHandler PlayerStopped;
        public event EventHandler<ProgressEventArgs> PlayerPaused;
        public event EventHandler PlayerFinished;
        public event EventHandler<ProgressEventArgs> PositionChanged;
        public event EventHandler<ProgressEventArgs> PositionSet;

        protected void RaisePlayerLoadedEvent(object sender, EventArgs e) =>
            InvokeActionOnMainThread(() => PlayerLoaded?.Invoke(sender, e));

        protected void RaisePlayerStartedEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PlayerStarted?.Invoke(sender, e));

        protected void RaisePlayerStoppedEvent(object sender, EventArgs e) =>
            InvokeActionOnMainThread(() => PlayerStopped?.Invoke(sender, e));

        protected void RaisePlayerPausedEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PlayerPaused?.Invoke(sender, e));

        protected void RaisePlayerFinishedEvent(object sender, EventArgs e) =>
            InvokeActionOnMainThread(() => PlayerFinished?.Invoke(sender, e));

        protected void RaiseProgressChangedEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PositionChanged?.Invoke(sender, e));

        protected void RaiseProgressSetEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PositionSet?.Invoke(sender, e));

        public bool RaiseEventInUiThread { get; set; } = true;

        public abstract int ProgressRefreshInterval { get; set; }

        public abstract PlayerStatus PlayerStatus { get; protected set; }
        public abstract int Duration { get; protected set; }
        public abstract int PlayTime { get; protected set; }

        
        public abstract void Play();
        public abstract void Pause();
        public abstract void Stop();
        public abstract void Replay();
        public abstract void SetTime(int ms, bool play = true);

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
                        RaiseProgressChangedEvent(this, new ProgressEventArgs(PlayTime, Duration));
                        sw.Restart();
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void InvokeActionOnMainThread(Action action)
        {
            if (RaiseEventInUiThread)
                UiContext.Send(obj => { action?.Invoke(); }, null);
            else
                action?.Invoke();
        }
    }
}