using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IIDXAudioGenerator.Services;
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
/// IIDX playback controller. Renders the IIDX chart + 2dx audio to a single
/// mixed WAV (via BemaniUtils <see cref="AudioRenderService"/>), then plays
/// that WAV through the same KeyAsio <see cref="StandaloneMusicTransport"/> as
/// the osu! path — so loop / rate / offset / volume all behave identically.
/// </summary>
/// <remarks>
/// IIDX charts have no concept of separately-scheduled hitsound events the way
/// osu! does: the 2dx file already contains every BGM and note sample, and the
/// chart tells the renderer when each plays. Mixing once up-front is what the
/// standalone <c>IIDXAudioGenerator</c> tool does, and reusing that path keeps
/// the audio identical between render and play.
/// </remarks>
public sealed class IidxMixPlayer : IMixPlayer
{
    private static readonly TimeSpan PositionFeedbackInterval = TimeSpan.FromMilliseconds(16);

    private readonly ILogger<IidxMixPlayer> _logger;
    private readonly BeatmapLoadResult _loadResult;
    private readonly IPlaybackEngine _engine;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly SoundTouchPlaybackRateProcessorFactory _rateFactory = new();

    private StandaloneMusicTransport? _musicTransport;
    private IMusicPlaybackSource? _musicSource;
    private OsuAudioSessionOptions? _sessionOptions;
    private readonly CancellableAsyncLoop _positionPumpLoop = new();
    private PlayStatus _playStatus = PlayStatus.Unknown;
    private int _manualOffset;
    private bool _isLooping;
    private float _preservePitchRateCompensationMilliseconds =
        PlaybackRateState.DefaultPreservePitchCompensationMilliseconds;
    private string? _renderedCachePath;

    public IidxMixPlayer(
        BeatmapLoadResult loadResult,
        IPlaybackEngine engine,
        AudioCacheManager audioCacheManager,
        ILogger<IidxMixPlayer> logger)
    {
        if (loadResult.IidxResources == null)
        {
            throw new ArgumentException(
                "BeatmapLoadResult.IidxResources must be set for IidxMixPlayer.", nameof(loadResult));
        }

        _loadResult = loadResult;
        _engine = engine;
        _audioCacheManager = audioCacheManager;
        _logger = logger;
    }

    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;

    public IWavePlayer? Device => _engine.CurrentDevice;
    public TimeSpan Duration => _musicTransport?.Duration ?? TimeSpan.Zero;
    public TimeSpan Position => _musicTransport?.Position ?? TimeSpan.Zero;
    public float PlaybackRate => _musicTransport?.RateState.Rate ?? 1f;
    public bool KeepTune => _musicTransport?.RateState.PreservePitch ?? false;

    public float PreservePitchRateCompensationMilliseconds
    {
        get => _preservePitchRateCompensationMilliseconds;
        set
        {
            _preservePitchRateCompensationMilliseconds = value;
            if (_sessionOptions != null)
            {
                _sessionOptions.PreservePitchRateCompensationMilliseconds = value;
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
            if (_musicTransport?.Source is { } source)
            {
                source.IsLooping = value;
            }
        }
    }

    public int ManualOffset
    {
        get => _manualOffset;
        set
        {
            _manualOffset = value;
            if (_sessionOptions != null)
            {
                _sessionOptions.ManualOffsetMilliseconds = value;
            }
        }
    }

    public int GeneralOffset
    {
        get => _sessionOptions?.GeneralOffsetMilliseconds ?? 0;
        set
        {
            if (_sessionOptions != null)
            {
                _sessionOptions.GeneralOffsetMilliseconds = value;
            }
        }
    }

    public async Task Initialize()
    {
        try
        {
            StartAudioEngine();
            _musicTransport = new StandaloneMusicTransport(_engine);

            var wavPath = await RenderChartToWavAsync().ConfigureAwait(false);
            _renderedCachePath = wavPath;

            _musicSource = await AudioFileMusicPlaybackSource.CreateAsync(
                _audioCacheManager,
                wavPath,
                _engine.SourceWaveFormat,
                _rateFactory).ConfigureAwait(false);

            await _musicTransport.LoadAsync(_musicSource, ownsSource: true).ConfigureAwait(false);
            ApplyLoopingToMusicSource();

            _sessionOptions = new OsuAudioSessionOptions
            {
                Resources = new BeatmapResources
                {
                    BeatmapFolder = Path.GetDirectoryName(wavPath) ?? string.Empty,
                    BeatmapFilename = Path.GetFileName(wavPath),
                    AudioFilename = Path.GetFileName(wavPath),
                    DefaultHitsoundFolder = AppPaths.Current.DefaultPath,
                    UserSkinFolder = AppPaths.Current.DefaultPath,
                },
                GeneralOffsetMilliseconds = AppSettings.Default?.Play?.GeneralActualOffset ?? 0,
                ManualOffsetMilliseconds = _manualOffset,
                PreservePitchRateCompensationMilliseconds = _preservePitchRateCompensationMilliseconds,
            };

            SynchronizeVolumeSettings();

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
        var transport = RequireTransport();
        if (PlayStatus == PlayStatus.Playing) return;
        if (PlayStatus == PlayStatus.Finished)
        {
            await SkipToAsync(TimeSpan.Zero).ConfigureAwait(false);
        }

        await transport.PlayAsync().ConfigureAwait(false);
        StartPositionPump();
        RaisePositionUpdated(Position);
        PlayStatus = PlayStatus.Playing;
    }

    public async Task PauseAsync()
    {
        var transport = RequireTransport();
        if (PlayStatus == PlayStatus.Paused) return;
        await StopPositionPumpAsync().ConfigureAwait(false);
        await transport.PauseAsync().ConfigureAwait(false);
        RaisePositionUpdated(Position);
        PlayStatus = PlayStatus.Paused;
    }

    public Task StopAsync() => StopCoreAsync();

    private async Task StopCoreAsync()
    {
        var transport = RequireTransport();
        await StopPositionPumpAsync().ConfigureAwait(false);
        await transport.StopAsync().ConfigureAwait(false);
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
        var transport = RequireTransport();
        var previousStatus = PlayStatus;
        PlayStatus = PlayStatus.Reposition;
        await transport.SeekAsync(time).ConfigureAwait(false);
        RaisePositionUpdated(time);

        if (previousStatus == PlayStatus.Playing)
        {
            await transport.PlayAsync().ConfigureAwait(false);
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

        var transport = RequireTransport();
        await transport.SetPlaybackRateAsync(new PlaybackRateState(rate, keepTune)).ConfigureAwait(false);
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
                if (_musicTransport != null)
                {
                    await _musicTransport.DisposeAsync().ConfigureAwait(false);
                }
            },
            _logger,
            "Error while disposing IidxMixPlayer.").ConfigureAwait(false);

        TryDeleteRenderedCache();
    }

    private async Task<string> RenderChartToWavAsync()
    {
        var resources = _loadResult.IidxResources!;
        var cacheDir = Path.Combine(AppPaths.Current.CachePath, "iidx");
        Directory.CreateDirectory(cacheDir);

        var musicId = resources.MusicId;
        var difficulty = resources.Difficulty;
        var cacheKey = $"{musicId:D5}-{difficulty}.wav";
        var outputPath = Path.Combine(cacheDir, cacheKey);

        if (!File.Exists(outputPath))
        {
            var renderService = new AudioRenderService(LoggerFactory.Create(builder => { }));
            var bgmVolumeFactor = ComputeBgmVolumeFactor(_loadResult.Beatmap.IidxBgmVolume);

            await renderService.RenderAsync(
                resources.Chart,
                new System.Collections.Generic.List<ReadOnlyMemory<byte>>(resources.AudioBlocks),
                outputPath,
                randomRange: 0,
                sigmaFactor: 3,
                bgmVolumeFactor: bgmVolumeFactor).ConfigureAwait(false);
        }

        return outputPath;
    }

    private static float ComputeBgmVolumeFactor(int? bgmVolume)
    {
        if (bgmVolume == null || bgmVolume <= 0) return 1.27f;
        var factor = bgmVolume.Value / 100f;
        if (factor < 0.01f) factor = 0.01f;
        if (factor > 4f) factor = 4f;
        return factor;
    }

    private StandaloneMusicTransport RequireTransport()
    {
        return _musicTransport ?? throw new InvalidOperationException(
            $"{nameof(IidxMixPlayer)}.{nameof(Initialize)} must be called before using playback operations.");
    }

    private void StartAudioEngine()
    {
        OsuPlayerAudioDevicePolicy.StartDevice(_engine, AppSettings.Default?.Play?.DeviceDescription);
        _engine.LimiterType = (LimiterType)(AppSettings.Default?.Volume.LimiterType ?? LimiterTypeSetting.Master);
    }

    private void SynchronizeVolumeSettings()
    {
        var volume = AppSettings.Default?.Volume;
        if (volume == null) return;

        _engine.MainVolume = volume.Main;
        _engine.MusicVolume = volume.Music;
        _engine.EffectVolume = 1;
        _engine.LimiterType = (LimiterType)volume.LimiterType;
    }

    private void Volume_PropertyChanged(object? sender, PropertyChangedEventArgs? e) => SynchronizeVolumeSettings();

    private void ApplyLoopingToMusicSource()
    {
        if (_musicTransport?.Source is { } source)
        {
            source.IsLooping = _isLooping;
        }
    }

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

    private ValueTask StopPositionPumpAsync() => _positionPumpLoop.StopAsync();

    private void RaisePositionUpdated(TimeSpan position) => PositionUpdated?.Invoke(position);

    private void TryDeleteRenderedCache()
    {
        try
        {
            if (_renderedCachePath != null && File.Exists(_renderedCachePath))
            {
                File.Delete(_renderedCachePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete IIDX render cache: {Path}", _renderedCachePath);
        }
    }
}