using Milky.OsuPlayer.Common.Player;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Core;

namespace Milky.OsuPlayer.Media.Audio
{
    public abstract class Player : IPlayer, IDisposable
    {
        private static readonly SynchronizationContext UiContext;
        private PlayStatus _playStatus;

        public event Action<PlayStatus> PlayStatusChanged;
        public event EventHandler PlayerLoaded;
        public event EventHandler<ProgressEventArgs> PlayerStarted;
        public event EventHandler PlayerStopped;
        public event EventHandler<ProgressEventArgs> PlayerPaused;
        public event EventHandler PlayerFinished;
        public event EventHandler<ProgressEventArgs> PositionChanged;
        public event EventHandler<ProgressEventArgs> PositionSet;

        protected virtual void RaisePlayerLoadedEvent(object sender, EventArgs e) =>
            InvokeActionOnMainThread(() => PlayerLoaded?.Invoke(sender, e));

        protected virtual void RaisePlayerStartedEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PlayerStarted?.Invoke(sender, e));

        protected virtual void RaisePlayerStoppedEvent(object sender, EventArgs e) =>
            InvokeActionOnMainThread(() => PlayerStopped?.Invoke(sender, e));

        protected virtual void RaisePlayerPausedEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PlayerPaused?.Invoke(sender, e));

        protected virtual void RaisePlayerFinishedEvent(object sender, EventArgs e) =>
            InvokeActionOnMainThread(() => PlayerFinished?.Invoke(sender, e));

        protected virtual void RaiseProgressChangedEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PositionChanged?.Invoke(sender, e));

        protected virtual void RaiseProgressSetEvent(object sender, ProgressEventArgs e) =>
            InvokeActionOnMainThread(() => PositionSet?.Invoke(sender, e));

        public virtual bool RaiseEventInUiThread { get; set; } = true;

        public abstract int ProgressRefreshInterval { get; set; }

        public virtual PlayStatus PlayStatus
        {
            get => _playStatus;
            protected set
            {
                if (Equals(value, _playStatus)) return;
                _playStatus = value;
                PlayStatusChanged?.Invoke(value);
            }
        }

        public abstract TimeSpan Duration { get; protected set; }
        public abstract TimeSpan PlayTime { get; protected set; }

        public abstract Task InitializeAsync();

        public void TogglePlay()
        {
            if (PlayStatus == PlayStatus.Ready || PlayStatus == PlayStatus.Finished || PlayStatus == PlayStatus.Paused)
            {
                Play();
            }
            else if (PlayStatus == PlayStatus.Playing)
            {
                Pause();
            }
        }

        public abstract void Play();
        public abstract void Pause();
        public abstract void Stop();
        public abstract void Replay();
        public abstract void SetTime(TimeSpan time, bool play = true);

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

        private void InvokeActionOnMainThread(Action action)
        {
            if (RaiseEventInUiThread)
                UiContext.Send(obj => { action?.Invoke(); }, null);
            else
                action?.Invoke();
        }

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
                        PlayStatus == PlayStatus.Playing)
                    {
                        oldT = newT;
                        RaiseProgressChangedEvent(this, new ProgressEventArgs(PlayTime, Duration));
                        sw.Restart();
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public virtual void Dispose()
        {
            PlayerLoaded = null;
            PlayerStarted = null;
            PlayerStopped = null;
            PlayerPaused = null;
            PlayerFinished = null;
            PositionChanged = null;
            PositionSet = null;
        }
    }
}