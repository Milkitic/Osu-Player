using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Audio.Rules;
using OsuPlayer.Media.Audio.SoundTouch;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Infrastructure;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Concrete player that wires the KeyAsio engine to an
/// <see cref="OsuBeatmapAudioSession"/>. Exposes a flat
/// <see cref="IPlaybackController"/>-style API and an observable play status.
/// </summary>
/// <remarks>
/// Intentionally thin: business rules live in <see cref="Rules.NightcoreRules"/>,
/// persistence lives in <c>AppSettings</c>, and the previous direct write to
/// <c>SharedVm.Default</c> has been removed — the controller relays status
/// through <see cref="PlayStatusChanged"/> and the UI binds directly.
/// </remarks>
public sealed class OsuMixPlayer : IPlaybackController, IAsyncDisposable
{
    private readonly ILogger<OsuMixPlayer> _logger;

    private readonly IPlaybackEngine _engine;
    private StandaloneMusicTransport? _musicTransport;
    private readonly AudioCacheManager _audioCacheManager;
    private OsuBeatmapAudioSession? _session;

    private OsuFile _osuFile;
    private string _sourceFolder;
    private OsuAudioSessionOptions? _sessionOptions;
    private readonly CancellableAsyncLoop _positionPumpLoop = new();
    private PlayStatus _playStatus = PlayStatus.Unknown;
    private int _manualOffset;

    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;

    public OsuMixPlayer(OsuFile osuFile, string sourceFolder, IPlaybackEngine engine, AudioCacheManager audioCacheManager, ILogger<OsuMixPlayer> logger)
    {
        _osuFile = osuFile;
        _sourceFolder = sourceFolder;
        _engine = engine;
        _audioCacheManager = audioCacheManager;
        _logger = logger;
    }

    public IWavePlayer? Device => _engine.CurrentDevice;
    public TimeSpan Duration => _session?.Duration ?? TimeSpan.Zero;
    public TimeSpan Position => _session?.Position ?? TimeSpan.Zero;
    public float PlaybackRate => _musicTransport?.RateState.Rate ?? 1f;
    public bool KeepTune => _musicTransport?.RateState.PreservePitch ?? false;

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
            _session?.ManualOffsetMilliseconds = value;
        }
    }

    public int GeneralOffset
    {
        get => _session?.GeneralOffsetMilliseconds ?? 0;
        set => _session?.GeneralOffsetMilliseconds = value;
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
            SynchronizeVolumeSettings();

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
            _logger.LogError(ex, "Error while initializing KeyAsio osu player.");
            throw;
        }
    }

    public async Task Reload(OsuFile osuFile, string sourceFolder)
    {
        await StopAsync().ConfigureAwait(false);
        _osuFile = osuFile;
        _sourceFolder = sourceFolder;
        _sessionOptions = CreateSessionOptions();
        SynchronizeVolumeSettings();
        var session = RequireSession();
        await session.LoadAsync(_osuFile, _sessionOptions).ConfigureAwait(false);
        PlayStatus = PlayStatus.Ready;
    }

    public async Task PlayAsync()
    {
        var session = RequireSession();
        if (PlayStatus == PlayStatus.Playing) return;
        if (PlayStatus == PlayStatus.Finished)
        {
            await SkipToAsync(TimeSpan.Zero).ConfigureAwait(false);
        }

        await session.PlayAsync().ConfigureAwait(false);
        StartPositionPump();
        RaisePositionUpdated(Position);
        PlayStatus = PlayStatus.Playing;
    }

    public Task PauseAsync()
    {
        if (PlayStatus == PlayStatus.Paused) return Task.CompletedTask;

        return PauseCoreAsync();
    }

    private async Task PauseCoreAsync()
    {
        var session = RequireSession();
        await StopPositionPumpAsync().ConfigureAwait(false);
        await session.PauseAsync().ConfigureAwait(false);
        RaisePositionUpdated(Position);
        PlayStatus = PlayStatus.Paused;
    }

    public Task StopAsync()
    {
        return StopCoreAsync();
    }

    private async Task StopCoreAsync()
    {
        var session = RequireSession();
        await StopPositionPumpAsync().ConfigureAwait(false);
        await session.StopAsync().ConfigureAwait(false);
        RaisePositionUpdated(TimeSpan.Zero);
        PlayStatus = PlayStatus.Paused;
    }

    public async Task RestartAsync()
    {
        await StopAsync().ConfigureAwait(false);
        await PlayAsync().ConfigureAwait(false);
    }

    public Task TogglePlayAsync()
    {
        return PlayStatus switch
        {
            PlayStatus.Ready or PlayStatus.Finished or PlayStatus.Paused => PlayAsync(),
            PlayStatus.Playing => PauseAsync(),
            _ => Task.CompletedTask,
        };
    }

    public async Task SetTimeAsync(double time, bool play)
    {
        await SkipToAsync(TimeSpan.FromMilliseconds(time)).ConfigureAwait(false);
        if (play && PlayStatus != PlayStatus.Playing)
        {
            await PlayAsync().ConfigureAwait(false);
        }
    }

    public async Task SkipToAsync(TimeSpan time)
    {
        var session = RequireSession();
        var previousStatus = PlayStatus;
        PlayStatus = PlayStatus.Reposition;
        await session.SeekAsync(time).ConfigureAwait(false);
        RaisePositionUpdated(time);

        if (previousStatus == PlayStatus.Playing)
        {
            await session.PlayAsync().ConfigureAwait(false);
            StartPositionPump();
            PlayStatus = PlayStatus.Playing;
        }
        else
        {
            PlayStatus = previousStatus switch
            {
                PlayStatus.Unknown => PlayStatus.Ready,
                PlayStatus.Finished => PlayStatus.Paused,
                _ => previousStatus,
            };
        }
    }

    public async Task SetPlaybackRate(float rate, bool keepTune)
    {
        AppSettings.Default.Play.PlaybackRate = rate;
        AppSettings.Default.Play.PlayUseTempo = keepTune;

        var session = RequireSession();
        var enableNightcoreBeats = NightcoreRules.ShouldEnableNightcoreBeats(rate, keepTune);
        await session.SetNightcoreBeatsAsync(enableNightcoreBeats).ConfigureAwait(false);
        await session.SetPlaybackRateAsync(new PlaybackRateState(rate, keepTune)).ConfigureAwait(false);
    }

    public async Task SetPlayMod(PlayModifier modifier)
    {
        switch (modifier)
        {
            case PlayModifier.None:
                await SetPlaybackRate(1, false).ConfigureAwait(false);
                break;
            case PlayModifier.DoubleTime:
                await SetPlaybackRate(NightcoreRules.NightcoreRate, true).ConfigureAwait(false);
                break;
            case PlayModifier.NightCore:
                await SetPlaybackRate(NightcoreRules.NightcoreRate, false).ConfigureAwait(false);
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

        var session = _session;
        await SafeStopExtensions.TryAsync(
            async () =>
            {
                await StopPositionPumpAsync().ConfigureAwait(false);
                await _positionPumpLoop.DisposeAsync().ConfigureAwait(false);
                if (session != null)
                {
                    session.Finished -= Session_Finished;
                    await session.DisposeAsync().ConfigureAwait(false);
                }
            },
            _logger,
            "Error while disposing OsuMixPlayer.").ConfigureAwait(false);
    }

    private OsuBeatmapAudioSession RequireSession()
    {
        return _session ?? throw new InvalidOperationException(
            $"{nameof(OsuMixPlayer)}.{nameof(Initialize)} must be called before using playback operations.");
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
        _engine.LimiterType = (KeyAsio.Core.Audio.LimiterType)(AppSettings.Default?.Volume.LimiterType ?? Core.Configuration.LimiterTypeSetting.Master);
    }

    private OsuAudioSessionOptions CreateSessionOptions()
    {
        var beatmapFilename = ResolveBeatmapFilename();
        var playSection = AppSettings.Default?.Play;

        return new OsuAudioSessionOptions
        {
            Resources = new BeatmapResources
            {
                BeatmapFolder = _sourceFolder,
                BeatmapFilename = beatmapFilename,
                AudioFilename = _osuFile.General?.AudioFilename ?? string.Empty,
                DefaultHitsoundFolder = AppPaths.Current.DefaultPath,
                UserSkinFolder = AppPaths.Current.DefaultPath,
            },
            GeneralOffsetMilliseconds = playSection?.GeneralActualOffset ?? 0,
            ManualOffsetMilliseconds = ManualOffset,
            EnableNightcoreBeats = NightcoreRules.ShouldEnableNightcoreBeats(
                playSection?.PlaybackRate ?? 1,
                playSection?.PlayUseTempo ?? false),
        };
    }

    private string ResolveBeatmapFilename()
    {
        if (_osuFile is LocalOsuFile localOsuFile)
        {
            return Path.GetFileName(localOsuFile.OriginalPath) ?? string.Empty;
        }

        return Directory.EnumerateFiles(_sourceFolder, "*.osu", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .FirstOrDefault() ?? string.Empty;
    }

    private void SynchronizeVolumeSettings()
    {
        var volume = AppSettings.Default?.Volume;
        if (volume == null || _sessionOptions == null) return;

        _engine.MainVolume = volume.Main;
        _engine.MusicVolume = volume.Music;
        _engine.EffectVolume = 1;
        _engine.LimiterType = (KeyAsio.Core.Audio.LimiterType)volume.LimiterType;

        _sessionOptions.HitsoundVolume = volume.Hitsound;
        _sessionOptions.SampleVolume = volume.Sample;
        _sessionOptions.BalanceFactor = volume.BalanceFactor / 100;
        _sessionOptions.BalanceMode = (KeyAsio.Core.Audio.SampleProviders.BalancePans.BalanceMode)volume.BalanceMode;
        _session?.ApplyOptions(_sessionOptions);
    }

    private void Volume_PropertyChanged(object? sender, PropertyChangedEventArgs? e)
    {
        SynchronizeVolumeSettings();
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
