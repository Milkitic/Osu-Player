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
using Milky.OsuPlayer.Media.Audio.TrackProvider;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio
{
    public sealed class ComponentPlayer : Player, IDisposable
    {
        private string _filePath;
        private int _stopCount;

        protected override string Flag { get; } = nameof(ComponentPlayer);
        public override int ProgressRefreshInterval { get; set; } = 500;

        private static IWavePlayer _outputDevice;
        private static AudioPlaybackEngine _engine;

        public OsuFile OsuFile { get; private set; }
        internal HitsoundPlayer HitsoundPlayer { get; private set; }
        internal SampleTrackPlayer SampleTrackPlayer { get; private set; }
        internal MusicPlayer MusicPlayer { get; private set; }

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public override PlayStatus PlayStatus
        {
            get => HitsoundPlayer?.PlayStatus ?? PlayStatus.Paused;
            protected set => throw new InvalidOperationException();
        }

        public override TimeSpan Duration
        {
            get => MathEx.Max(HitsoundPlayer?.Duration ?? TimeSpan.Zero, SampleTrackPlayer?.Duration ?? TimeSpan.Zero);
            protected set => throw new InvalidOperationException();
        }

        public override TimeSpan PlayTime
        {
            get => HitsoundPlayer?.PlayTime ?? TimeSpan.Zero;
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

        internal static ComponentPlayer Current { get; private set; }

        public ComponentPlayer(string filePath, OsuFile osuFile)
        {
            Current?.Dispose();
            Current = this;
            _filePath = filePath;
            OsuFile = osuFile;
        }

        public override async Task InitializeAsync()
        {
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
            HitsoundPlayer.PlayStatusChanged += s => base.PlayStatus = s;
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
            Console.WriteLine($"{nameof(_stopCount)}: {_stopCount};{sender}");
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

        public override void SetTime(TimeSpan time, bool play = true)
        {
            MusicPlayer.SetTime(time, play);
            HitsoundPlayer.SetTime(time, play);
            SampleTrackPlayer.SetTime(time, play);
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
                    SetPlaybackRate(AppSettings.Default.Play.PlaybackRate,
                        MusicPlayer.PlayStatus == PlayStatus.Playing);
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
