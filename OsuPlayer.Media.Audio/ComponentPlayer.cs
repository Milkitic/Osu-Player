using Milky.OsuPlayer.Common.Player;
using OSharp.Beatmap;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio.Core;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio
{
    public sealed class ComponentPlayer : Player, IDisposable
    {
        private string _filePath;
        private int _stopCount;

        public override int ProgressRefreshInterval { get; set; } = 500;

        private static IWavePlayer _outputDevice;
        private static AudioPlaybackEngine _engine;

        public OsuFile OsuFile { get; private set; }
        internal HitsoundPlayer HitsoundPlayer { get; private set; }
        internal SampleTrackPlayer SampleTrackPlayer { get; private set; }
        internal MusicPlayer MusicPlayer { get; private set; }

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public override PlayerStatus PlayerStatus
        {
            get => HitsoundPlayer?.PlayerStatus ?? PlayerStatus.Stopped;
            protected set => throw new InvalidOperationException();
        }

        public override int Duration
        {
            get => Math.Max(HitsoundPlayer?.Duration ?? 0, SampleTrackPlayer?.Duration ?? 0);
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
            set
            {
                HitsoundPlayer.SingleOffset = value;
                SampleTrackPlayer.SingleOffset = value;
            }
        }

        public static ComponentPlayer Current { get; set; }

        public ComponentPlayer(string filePath, OsuFile osuFile)
        {
            _filePath = filePath;
            OsuFile = osuFile;
        }

        public override async Task InitializeAsync()
        {
            Current?.Dispose();
            Current = this;
            _outputDevice = DeviceProvider.CreateOrGetDefaultDevice();
            _engine = new AudioPlaybackEngine(_outputDevice);
            FileInfo fileInfo = new FileInfo(_filePath);
            DirectoryInfo dirInfo = fileInfo.Directory;
            FileInfo musicInfo = new FileInfo(Path.Combine(dirInfo.FullName, OsuFile.General.AudioFilename));
            MusicPlayer = new MusicPlayer(_engine, _outputDevice, musicInfo.FullName);
            await MusicPlayer.InitializeAsync();

            HitsoundPlayer = new HitsoundPlayer(_engine, _filePath, OsuFile);
            SampleTrackPlayer = new SampleTrackPlayer(_engine, _filePath, OsuFile);

            await HitsoundPlayer.InitializeAsync();
            await SampleTrackPlayer.InitializeAsync();

            HitsoundPlayer.SetDuration(MusicPlayer.Duration);
            HitsoundPlayer.PlayerFinished += Players_OnFinished;
            SampleTrackPlayer.SetDuration(MusicPlayer.Duration);
            SampleTrackPlayer.PlayerFinished += Players_OnFinished;
            MusicPlayer.PlayerFinished += Players_OnFinished;
            AppSettings.Default.Play.PropertyChanged += Play_PropertyChanged;

            NotifyProgress(_cts.Token);

            RaisePlayerLoadedEvent(this, new EventArgs());
        }

        private void Players_OnFinished(object sender, EventArgs e)
        {
            _stopCount++;
            if (_stopCount < 3) return;

            ResetWithoutNotify();

            RaisePlayerFinishedEvent(this, new EventArgs());
        }

        public override void Play()
        {
            if (HitsoundPlayer.IsPlaying)
                return;

            _stopCount = 0;
            MusicPlayer.Play();
            HitsoundPlayer.Play();
            SampleTrackPlayer.Play();
            RaisePlayerStartedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Pause()
        {
            MusicPlayer.Pause();
            HitsoundPlayer.Pause();
            SampleTrackPlayer.Pause();

            RaisePlayerPausedEvent(this, new ProgressEventArgs(PlayTime, Duration));
        }

        public override void Stop()
        {
            ResetWithoutNotify();
            RaisePlayerStoppedEvent(this, new EventArgs());
        }

        private void ResetWithoutNotify()
        {
            HitsoundPlayer?.ResetWithoutNotify();
            SampleTrackPlayer?.ResetWithoutNotify();
            MusicPlayer?.ResetWithoutNotify();
        }

        public override void Replay()
        {
            Stop();
            Play();
        }

        public override void SetTime(int ms, bool play = true)
        {
            HitsoundPlayer.SetTime(ms, play);
            SampleTrackPlayer.SetTime(ms, play);
            if (play)
            {
                MusicPlayer.SetTime(ms);
                HitsoundPlayer.Play();
                SampleTrackPlayer.Play();
            }
            else
                MusicPlayer.SetTime(ms, false);
        }

        public void SetPlayMod(PlayMod mod)
        {
            switch (mod)
            {
                case PlayMod.None:
                    AppSettings.Default.Play.PlayUseTempo = true;
                    AppSettings.Default.Play.PlaybackRate = 1;
                    break;
                case PlayMod.DoubleTime:
                    AppSettings.Default.Play.PlayUseTempo = true;
                    AppSettings.Default.Play.PlaybackRate = 1.5f;
                    break;
                case PlayMod.NightCore:
                    AppSettings.Default.Play.PlayUseTempo = false;
                    AppSettings.Default.Play.PlaybackRate = 1.5f;
                    break;
                case PlayMod.HalfTime:
                    AppSettings.Default.Play.PlayUseTempo = true;
                    AppSettings.Default.Play.PlaybackRate = 0.75f;
                    break;
                case PlayMod.DayCore:
                    AppSettings.Default.Play.PlayUseTempo = false;
                    AppSettings.Default.Play.PlaybackRate = 0.75f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mod), mod, null);
            }

            AppSettings.SaveDefault();
        }

        private void Play_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.Play.PlayUseTempo):
                    SetTempoMode(AppSettings.Default.Play.PlayUseTempo);
                    break;
                case nameof(AppSettings.Play.PlaybackRate):
                    SetPlaybackRate(AppSettings.Default.Play.PlaybackRate, MusicPlayer.PlayerStatus == PlayerStatus.Playing);
                    break;
            }
        }

        public void SetTempoMode(bool useTempo)
        {
            HitsoundPlayer.SetTempoMode(useTempo);
            SampleTrackPlayer.SetTempoMode(useTempo);
            MusicPlayer.SetTempoMode(useTempo);
            SetTime(MusicPlayer.PlayTime);
        }

        public void SetPlaybackRate(float rate, bool b)
        {
            MusicPlayer.SetPlaybackRate(rate);
            HitsoundPlayer.SetPlaybackRate(rate, b);
            SampleTrackPlayer.SetPlaybackRate(rate, b);
            SetTime(MusicPlayer.PlayTime);
        }

        public override void Dispose()
        {
            base.Dispose();

            AppSettings.Default.Play.PropertyChanged -= Play_PropertyChanged;
            _engine?.Dispose();
            _outputDevice?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
            HitsoundPlayer?.Dispose();
            SampleTrackPlayer?.Dispose();
            MusicPlayer?.Dispose();
            Current = null;
        }
    }
}
