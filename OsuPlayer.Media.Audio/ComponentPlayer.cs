using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Music;
using OSharp.Beatmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio
{
    public sealed class ComponentPlayer : Player, IDisposable
    {
        private string _filePath;
        private int _stopCount;

        public override int ProgressRefreshInterval { get; set; }

        public OsuFile OsuFile { get; }
        internal HitsoundPlayer HitsoundPlayer { get; private set; }
        internal MusicPlayer MusicPlayer { get; private set; }

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public override PlayerStatus PlayerStatus
        {
            get => HitsoundPlayer?.PlayerStatus ?? PlayerStatus.Stopped;
            protected set => throw new InvalidOperationException();
        }

        public override int Duration
        {
            get => HitsoundPlayer?.Duration ?? 0;
            protected set => throw new InvalidOperationException();
        }

        public override int PlayTime
        {
            get => HitsoundPlayer?.PlayTime ?? 0;
            protected set => throw new InvalidOperationException();
        }

        public int HitsoundOffset
        {
            get => HitsoundPlayer.SingleOffset;
            set => HitsoundPlayer.SingleOffset = value;
        }

        public static ComponentPlayer Current { get; set; }

        private ComponentPlayer(string filePath, OsuFile osuFile)
        {
            Current?.Dispose();
            Current = this;
            _filePath = filePath;
            OsuFile = osuFile;
            HitsoundPlayer = new HitsoundPlayer(filePath, osuFile);

            FileInfo musicInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(filePath), OsuFile.General.AudioFilename));
            MusicPlayer = new MusicPlayer(musicInfo.FullName);

            HitsoundPlayer.SetDuration(MusicPlayer.Duration);
            HitsoundPlayer.PlayerFinished += Players_OnFinished;
            MusicPlayer.PlayerFinished += Players_OnFinished;

            NotifyProgress(_cts.Token);
            RaisePlayerLoadedEvent(this, new EventArgs());
        }

        private void Players_OnFinished(object sender, EventArgs e)
        {
            _stopCount++;
            if (_stopCount < 2) return;

            ResetWithoutNotify();

            RaisePlayerFinishedEvent(this, new EventArgs());
        }

        public static async Task<ComponentPlayer> InitializeAsync(string filePath, OsuFile osuFile)
        {
            return await Task.Run(() => new ComponentPlayer(filePath, osuFile));
        }

        public override void Play()
        {
            if (HitsoundPlayer.IsPlaying)
                return;

            _stopCount = 0;
            MusicPlayer.Play();
            HitsoundPlayer.Play();

            RaisePlayerStartedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Pause()
        {
            MusicPlayer.Pause();
            HitsoundPlayer.Pause();

            RaisePlayerPausedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Stop()
        {
            ResetWithoutNotify();
            RaisePlayerStoppedEvent(this, new EventArgs());
        }

        private void ResetWithoutNotify()
        {
            HitsoundPlayer.ResetWithoutNotify();
            MusicPlayer.ResetWithoutNotify();
        }

        public override void Replay()
        {
            Stop();
            Play();
        }

        public override void SetTime(int ms, bool play = true)
        {
            HitsoundPlayer.SetTime(ms, play);
            if (play)
            {
                MusicPlayer.SetTime(ms);
                HitsoundPlayer.Play();
            }
            else
                MusicPlayer.SetTime(ms, false);
        }

        public void SetPlayMod(PlayMod mod, bool play)
        {
            MusicPlayer.SetPlayMod(mod);
            HitsoundPlayer.SetPlayMod(mod, play);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            HitsoundPlayer?.Dispose();
            MusicPlayer?.Dispose();
            Current = null;
        }

        public static void DisposeAll()
        {
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
        }
    }
}
