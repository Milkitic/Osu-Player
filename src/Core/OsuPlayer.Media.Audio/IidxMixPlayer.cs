using System;
using System.ComponentModel;
using System.Threading.Tasks;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Iidx.Abstractions;
using OsuPlayer.Media.Audio.Rules;
using OsuPlayer.Media.Audio.SoundTouch;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Infrastructure;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// IIDX playback controller. Mirrors <see cref="OsuMixPlayer"/>'s shell but
/// delegates to <see cref="IidxBeatmapAudioSession"/>, which schedules decoded
/// 2dx blocks in real time with the same 12-second sliding precache window the
/// osu! path uses for hitsounds — no offline WAV render.
/// </summary>
public sealed class IidxMixPlayer : IMixPlayer
{
    private static readonly TimeSpan PositionFeedbackInterval = TimeSpan.FromMilliseconds(16);

    private readonly ILogger<IidxMixPlayer> _logger;
    private readonly BeatmapLoadResult _loadResult;
    private readonly IPlaybackEngine _engine;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly SoundTouchPlaybackRateProcessorFactory _rateFactory = new();

    private StandaloneMusicTransport? _musicTransport;
    private IidxBeatmapAudioSession? _session;
    private OsuAudioSessionOptions? _sessionOptions;
    private readonly CancellableAsyncLoop _positionPumpLoop = new();
    private PlayStatus _playStatus = PlayStatus.Unknown;
    private int _manualOffset;
    private bool _isLooping;
    private float _preservePitchRateCompensationMilliseconds =
        PlaybackRateState.DefaultPreservePitchCompensationMilliseconds;

    public IidxMixPlayer(
        BeatmapLoadResult loadResult,
        IPlaybackEngine engine,
        AudioCacheManager audioCacheManager,
        ILogger<IidxMixPlayer> logger)
    {
        if (loadResult.IidxResources is null)
        {
            throw new ArgumentException(
                "BeatmapLoadResult.IidxResources must be set for IidxMixPlayer.", nameof(loadResult));
        }

        _loadResult = loadResult;
        _resources = loadResult.IidxResources;
        _engine = engine;
        _audioCacheManager = audioCacheManager;
        _logger = logger;
    }

    private readonly IidxLoadedResources _resources;

    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;

    public IWavePlayer? Device => _engine.CurrentDevice;
    public TimeSpan Duration => _session?.Duration ?? TimeSpan.Zero;
    public TimeSpan Position => _session?.Position ?? TimeSpan.Zero;
    public float PlaybackRate => _session?.RateState.Rate ?? 1f;
    public bool KeepTune => _session?.RateState.PreservePitch ?? false;

    public float PreservePitchRateCompensationMilliseconds
    {
        get => _preservePitchRateCompensationMilliseconds;
        set
        {
            _preservePitchRateCompensationMilliseconds = value;
            if (_session != null)
            {
                _session.PreservePitchRateCompensationMilliseconds = value;
            }
        }
    }

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

    public bool IsLooping
    {
        get => _isLooping;
        set
        {
            _isLooping = value;
            if (_session != null)
            {
                _session.IsLooping = value;
            }
        }
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
        set => _session.GeneralOffsetMilliseconds = value;
    }

    public async Task Initialize()
    {
        try
        {
            StartAudioEngine();
            _musicTransport = new StandaloneMusicTransport(_engine);
            _session = new IidxBeatmapAudioSession(
                _engine, _musicTransport, _audioCacheManager,
                _resources, _rateFactory,
                _logger);

            _session.IsLooping = _isLooping;
            _session.Finished += Session_Finished;
            _sessionOptions = CreateSessionOptions();
            SynchronizeVolumeSettings();

            await _session.LoadAsync(_resources, _sessionOptions).ConfigureAwait(false);
            await SetPlaybackRate(
                AppSettings.Default?.Play?.PlaybackRate ?? 1,
                AppSettings.Default?.Play?.PlayUseTempo ?? false).ConfigureAwait(false);

            if (AppSettings.Default?.Volume != null)
            {
                AppSettings.Default.Volume.PropertyChanged += Volume_PropertyChanged;
            }

            PlayStatus = PlayStatus.Ready;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while initializing IIDX player.");
            throw;
        }
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

    public async Task PauseAsync()
    {
        if (PlayStatus == PlayStatus.Paused) return;
        var session = RequireSession();
        await StopPositionPumpAsync().ConfigureAwait(false);
        await session.PauseAsync().ConfigureAwait(false);
        RaisePositionUpdated(Position);
        PlayStatus = PlayStatus.Paused;
    }

    public Task StopAsync() => StopCoreAsync();

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
        var playSection = AppSettings.Default?.Play;
        if (playSection != null)
        {
            playSection.PlaybackRate = rate;
            playSection.PlayUseTempo = keepTune;
        }

        var session = RequireSession();
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

        await SafeStopExtensions.TryAsync(
            async () =>
            {
                await StopPositionPumpAsync().ConfigureAwait(false);
                await _positionPumpLoop.DisposeAsync().ConfigureAwait(false);
                if (_session != null)
                {
                    _session.Finished -= Session_Finished;
                    await _session.DisposeAsync().ConfigureAwait(false);
                }

                if (_musicTransport != null)
                {
                    await _musicTransport.DisposeAsync().ConfigureAwait(false);
                }
            },
            _logger,
            "Error while disposing IidxMixPlayer.").ConfigureAwait(false);
    }

    private IidxBeatmapAudioSession RequireSession()
    {
        return _session ?? throw new InvalidOperationException(
            $"{nameof(IidxMixPlayer)}.{nameof(Initialize)} must be called before using playback operations.");
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
        _engine.LimiterType = (LimiterType)(AppSettings.Default?.Volume.LimiterType ?? LimiterTypeSetting.Master);
    }

    private OsuAudioSessionOptions CreateSessionOptions()
    {
        var playSection = AppSettings.Default?.Play;
        return new OsuAudioSessionOptions
        {
            Resources = new BeatmapResources
            {
                BeatmapFolder = _loadResult.BaseFolder,
                BeatmapFilename = _loadResult.MapPath,
                AudioFilename = _loadResult.MusicPath ?? string.Empty,
                DefaultHitsoundFolder = AppPaths.Current.DefaultPath,
                UserSkinFolder = AppPaths.Current.DefaultPath,
            },
            GeneralOffsetMilliseconds = playSection?.GeneralActualOffset ?? 0,
            ManualOffsetMilliseconds = _manualOffset,
            PreservePitchRateCompensationMilliseconds = _preservePitchRateCompensationMilliseconds,
        };
    }

    private void SynchronizeVolumeSettings()
    {
        var volume = AppSettings.Default?.Volume;
        if (volume == null || _sessionOptions == null) return;

        _engine.MainVolume = volume.Main;
        _engine.MusicVolume = volume.Music;
        _engine.EffectVolume = 1;
        _engine.LimiterType = (LimiterType)volume.LimiterType;

        _sessionOptions.HitsoundVolume = volume.Hitsound;
        _sessionOptions.SampleVolume = volume.Sample;
        _sessionOptions.BalanceFactor = volume.BalanceFactor / 100;
        _sessionOptions.BalanceMode = (KeyAsio.Core.Audio.SampleProviders.BalancePans.BalanceMode)volume.BalanceMode;
        _session?.ApplyOptions(_sessionOptions);
    }

    private void Volume_PropertyChanged(object? sender, PropertyChangedEventArgs? e) => SynchronizeVolumeSettings();

    private void StartPositionPump()
    {
        _positionPumpLoop.Start(async ct =>
        {
            while (!ct.IsCancellationRequested)
            {
                RaisePositionUpdated(Position);
                await Task.Delay(PositionFeedbackInterval, ct).ConfigureAwait(false);
            }
        });
    }

    private void StopPositionPump() => _positionPumpLoop.Stop();
    private ValueTask StopPositionPumpAsync() => _positionPumpLoop.StopAsync();
    private void RaisePositionUpdated(TimeSpan position) => PositionUpdated?.Invoke(position);
}