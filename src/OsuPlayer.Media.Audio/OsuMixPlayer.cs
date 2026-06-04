using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Media.Audio.Infrastructure;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Media.Audio.SoundTouch;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio
{
    public sealed class OsuMixPlayer : IAsyncDisposable
    {
        public static OsuMixPlayer Current { get; private set; }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IPlaybackEngine _engine;
        private StandaloneMusicTransport _musicTransport;
        private readonly AudioCacheManager _audioCacheManager;
        private OsuBeatmapAudioSession _session;

        private OsuFile _osuFile;
        private string _sourceFolder;
        private OsuAudioSessionOptions _sessionOptions;
        private readonly CancellableAsyncLoop _positionPumpLoop = new();
        private PlayStatus _playStatus = PlayStatus.Unknown;
        private int _manualOffset;

        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan> PositionUpdated;

        public OsuMixPlayer(OsuFile osuFile, string sourceFolder, IPlaybackEngine engine, AudioCacheManager audioCacheManager)
        {
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;

            _engine = engine;
            _audioCacheManager = audioCacheManager;

            Current = this;
        }

        public IWavePlayer Device => _engine.CurrentDevice;
        public TimeSpan Duration => _session.Duration;
        public TimeSpan Position => _session.Position;
        public float PlaybackRate => _musicTransport.RateState.Rate;
        public bool KeepTune => _musicTransport.RateState.PreservePitch;

        public PlayStatus PlayStatus
        {
            get => _playStatus;
            private set
            {
                if (_playStatus == value) return;
                _playStatus = value;
                PlayStatusChanged?.Invoke(value);
            }
        }

        public float Volume
        {
            get => _engine.MainVolume;
            set => _engine.MainVolume = value;
        }

        public int ManualOffset
        {
            get => _manualOffset;
            set
            {
                _manualOffset = value;
                _session.ManualOffsetMilliseconds = value;
                if (_sessionOptions != null)
                {
                    _sessionOptions.ManualOffsetMilliseconds = value;
                }
            }
        }

        public async Task Initialize()
        {
            try
            {
                ConfigureSoundTouchRuntime();
                StartAudioEngine();
                _musicTransport = new StandaloneMusicTransport(_engine);
                _session = new OsuBeatmapAudioSession(_engine, _musicTransport, _audioCacheManager,
                    new SoundTouchPlaybackRateProcessorFactory());
                _session.Finished += Session_Finished;
                _sessionOptions = CreateSessionOptions();
                ApplyVolumeSettings();

                await _session.LoadAsync(_osuFile, _sessionOptions).ConfigureAwait(false);
                await SetPlaybackRate(AppSettings.Default?.Play?.PlaybackRate ?? 1,
                    AppSettings.Default?.Play?.PlayUseTempo ?? false).ConfigureAwait(false);

                if (AppSettings.Default?.Volume != null)
                {
                    AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
                }

                PlayStatus = PlayStatus.Ready;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while initializing KeyAsio osu player.");
                throw;
            }
        }

        public async Task Reload(OsuFile osuFile, string sourceFolder)
        {
            await Stop().ConfigureAwait(false);
            _osuFile = osuFile;
            _sourceFolder = sourceFolder;
            _sessionOptions = CreateSessionOptions();
            ApplyVolumeSettings();
            await _session.LoadAsync(_osuFile, _sessionOptions).ConfigureAwait(false);
            PlayStatus = PlayStatus.Ready;
        }

        public async Task Play()
        {
            if (PlayStatus == PlayStatus.Playing) return;
            if (PlayStatus == PlayStatus.Finished)
            {
                await SkipTo(TimeSpan.Zero).ConfigureAwait(false);
            }

            await _session.PlayAsync().ConfigureAwait(false);
            StartPositionPump();
            RaisePositionUpdated(Position);
            PlayStatus = PlayStatus.Playing;
        }

        public async Task Pause()
        {
            if (PlayStatus == PlayStatus.Paused) return;

            await StopPositionPumpAsync().ConfigureAwait(false);
            await _session.PauseAsync().ConfigureAwait(false);
            RaisePositionUpdated(Position);
            PlayStatus = PlayStatus.Paused;
        }

        public async Task Stop()
        {
            await StopPositionPumpAsync().ConfigureAwait(false);
            await _session.StopAsync().ConfigureAwait(false);
            RaisePositionUpdated(TimeSpan.Zero);
            PlayStatus = PlayStatus.Paused;
        }

        public async Task Restart()
        {
            await Stop().ConfigureAwait(false);
            await Play().ConfigureAwait(false);
        }

        public async Task SkipTo(TimeSpan time)
        {
            var previousStatus = PlayStatus;
            PlayStatus = PlayStatus.Reposition;
            await _session.SeekAsync(time).ConfigureAwait(false);
            RaisePositionUpdated(time);

            if (previousStatus == PlayStatus.Playing)
            {
                await _session.PlayAsync().ConfigureAwait(false);
                StartPositionPump();
                PlayStatus = PlayStatus.Playing;
            }
            else
            {
                PlayStatus = previousStatus == PlayStatus.Unknown ? PlayStatus.Ready : previousStatus;
            }
        }

        public async Task SetPlaybackRate(float rate, bool keepTune)
        {
            AppSettings.Default.Play.PlaybackRate = rate;
            AppSettings.Default.Play.PlayUseTempo = keepTune;

            var enableNightcoreBeats = Math.Abs(rate - 1.5f) < 0.001f && !keepTune;
            await _session.SetNightcoreBeatsAsync(enableNightcoreBeats).ConfigureAwait(false);
            await _session.SetPlaybackRateAsync(new PlaybackRateState(rate, keepTune)).ConfigureAwait(false);
        }

        public async Task SetPlayMod(PlayModifier modifier)
        {
            switch (modifier)
            {
                case PlayModifier.None:
                    await SetPlaybackRate(1, false).ConfigureAwait(false);
                    break;
                case PlayModifier.DoubleTime:
                    await SetPlaybackRate(1.5f, true).ConfigureAwait(false);
                    break;
                case PlayModifier.NightCore:
                    await SetPlaybackRate(1.5f, false).ConfigureAwait(false);
                    break;
                case PlayModifier.HalfTime:
                    await SetPlaybackRate(0.75f, true).ConfigureAwait(false);
                    break;
                case PlayModifier.DayCore:
                    await SetPlaybackRate(0.75f, false).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null);
            }

            AppSettings.SaveDefault();
        }

        public async ValueTask DisposeAsync()
        {
            if (AppSettings.Default?.Volume != null)
            {
                AppSettings.Default.Volume.PropertyChanged -= Volume_PropertyChanged;
            }

            await StopPositionPumpAsync().ConfigureAwait(false);
            await _positionPumpLoop.DisposeAsync().ConfigureAwait(false);
            _session.Finished -= Session_Finished;
            await _session.DisposeAsync().ConfigureAwait(false);
        }

        private void Session_Finished()
        {
            StopPositionPump();
            RaisePositionUpdated(Duration);
            PlayStatus = PlayStatus.Finished;
        }

        private void StartAudioEngine()
        {
            OsuPlayerAudioDevicePolicy.StartDevice(_engine, AppSettings.Default?.Play?.DeviceDescription);
        }

        private OsuAudioSessionOptions CreateSessionOptions()
        {
            var beatmapFilename = _osuFile is LocalOsuFile localOsuFile
                ? Path.GetFileName(localOsuFile.OriginalPath)
                : Directory.EnumerateFiles(_sourceFolder, "*.osu", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .FirstOrDefault() ?? string.Empty;

            return new OsuAudioSessionOptions
            {
                BeatmapFolder = _sourceFolder,
                BeatmapFilename = beatmapFilename,
                AudioFilename = _osuFile.General.AudioFilename,
                DefaultHitsoundFolder = Domain.DefaultPath,
                UserSkinFolder = Domain.DefaultPath,
                GeneralOffsetMilliseconds = AppSettings.Default?.Play?.GeneralActualOffset ?? 0,
                ManualOffsetMilliseconds = ManualOffset,
                EnableNightcoreBeats = Math.Abs((AppSettings.Default?.Play?.PlaybackRate ?? 1) - 1.5f) < 0.001f &&
                                       !(AppSettings.Default?.Play?.PlayUseTempo ?? false)
            };
        }

        private void ApplyVolumeSettings()
        {
            if (AppSettings.Default?.Volume == null || _sessionOptions == null) return;

            _engine.MainVolume = AppSettings.Default.Volume.Main;
            _engine.MusicVolume = AppSettings.Default.Volume.Music;
            _engine.EffectVolume = 1;

            _sessionOptions.HitsoundVolume = AppSettings.Default.Volume.Hitsound;
            _sessionOptions.SampleVolume = AppSettings.Default.Volume.Sample;
            _sessionOptions.BalanceFactor = AppSettings.Default.Volume.BalanceFactor / 100;
            _session.ApplyOptions(_sessionOptions);
        }

        private void Volume_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.Default.Volume.Main):
                    _engine.MainVolume = AppSettings.Default.Volume.Main;
                    break;
                case nameof(AppSettings.Default.Volume.Music):
                    _engine.MusicVolume = AppSettings.Default.Volume.Music;
                    break;
                case nameof(AppSettings.Default.Volume.Hitsound):
                case nameof(AppSettings.Default.Volume.Sample):
                case nameof(AppSettings.Default.Volume.BalanceFactor):
                    ApplyVolumeSettings();
                    break;
            }
        }

        private void StartPositionPump()
        {
            _positionPumpLoop.Start(async ct =>
            {
                while (!ct.IsCancellationRequested)
                {
                    RaisePositionUpdated(Position);
                    await Task.Delay(250, ct).ConfigureAwait(false);
                }
            });
        }

        private void StopPositionPump()
        {
            _positionPumpLoop.Stop();
        }

        private ValueTask StopPositionPumpAsync()
        {
            return _positionPumpLoop.StopAsync();
        }

        private void RaisePositionUpdated(TimeSpan position)
        {
            PositionUpdated?.Invoke(position);
        }

        private static void ConfigureSoundTouchRuntime()
        {
            SoundTouchRuntime.Configure(Path.Combine(AppContext.BaseDirectory, "runtimes"));
        }
    }
}
