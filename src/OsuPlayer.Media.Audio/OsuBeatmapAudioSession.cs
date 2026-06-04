using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.OsuAudio.Hitsounds;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using KeyAsio.Core.OsuAudio.Timeline;
using Microsoft.Extensions.Logging;
using Milky.OsuPlayer.Media.Audio.Infrastructure;

namespace Milky.OsuPlayer.Media.Audio;

internal sealed class OsuBeatmapAudioSession : IPlaybackClock, IAsyncDisposable
{
    private static readonly TimeSpan MinimumSchedulerDelay = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan MaximumSchedulerDelay = TimeSpan.FromMilliseconds(50);
    private const int CacheWindowMilliseconds = 12_000;
    private const int CacheAdvanceMilliseconds = 8_000;

    private readonly IPlaybackEngine _playbackEngine;
    private readonly StandaloneMusicTransport _musicTransport;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly IPlaybackRateProcessorFactory _rateProcessorFactory;
    private readonly OsuPlaybackEventDispatcher _eventDispatcher;
    private readonly OsuPlaybackEventAudioCache _eventAudioCache;
    private readonly PlaybackEventTimelineScheduler _timelineScheduler = new();
    private readonly List<PlaybackEvent> _eventBuffer = new(128);
    private readonly CancellableAsyncLoop _schedulerLoop = new();
    private readonly ILogger? _logger;

    private IReadOnlyList<PlaybackEvent> _playbackEvents = [];
    private OsuFile _osuFile = null!;
    private OsuAudioSessionOptions _options = null!;
    private int _nextCacheStart;

    public OsuBeatmapAudioSession(
        IPlaybackEngine playbackEngine,
        StandaloneMusicTransport musicTransport,
        AudioCacheManager audioCacheManager,
        IPlaybackRateProcessorFactory? rateProcessorFactory = null,
        ILogger? logger = null)
    {
        _playbackEngine = playbackEngine;
        _musicTransport = musicTransport;
        _audioCacheManager = audioCacheManager;
        _rateProcessorFactory = rateProcessorFactory ?? NoPlaybackRateProcessorFactory.Instance;
        _logger = logger;
        var effectBus = new OsuEffectPlaybackBus(playbackEngine.EffectMixer);
        _eventDispatcher = new OsuPlaybackEventDispatcher(effectBus, logger);
        _eventAudioCache = new OsuPlaybackEventAudioCache(audioCacheManager, logger);
    }

    public event Action? Finished;

    public TimeSpan Position => _musicTransport.Position;
    public TimeSpan Duration => _musicTransport.Duration;
    public PlaybackRateState RateState => _musicTransport.RateState;
    public bool IsRunning => _musicTransport.IsRunning;
    public bool SupportsPlaybackRateChange => _musicTransport.SupportsPlaybackRateChange;

    public int ManualOffsetMilliseconds
    {
        get => _options?.ManualOffsetMilliseconds ?? 0;
        set
        {
            if (_options != null)
            {
                _options.ManualOffsetMilliseconds = value;
            }
        }
    }

    public async Task LoadAsync(OsuFile osuFile, OsuAudioSessionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(osuFile);
        ArgumentNullException.ThrowIfNull(options);

        await ClearAsync(cancellationToken).ConfigureAwait(false);

        _osuFile = osuFile;
        _options = options;
        var resources = options.Resources;

        var musicPath = Path.Combine(resources.BeatmapFolder, resources.AudioFilename);
        var musicSource = await AudioFileMusicPlaybackSource.CreateAsync(
            _audioCacheManager,
            musicPath,
            _playbackEngine.SourceWaveFormat,
            _rateProcessorFactory,
            cancellationToken).ConfigureAwait(false);

        await _musicTransport.LoadAsync(musicSource, ownsSource: true, cancellationToken).ConfigureAwait(false);

        _eventAudioCache.SetContext(resources.BeatmapFolder, resources.UserSkinFolder,
            resources.DefaultHitsoundFolder, _playbackEngine.SourceWaveFormat);
        ApplyOptions(options);

        _playbackEvents = await BuildPlaybackEventsAsync(osuFile, options, cancellationToken).ConfigureAwait(false);
        _timelineScheduler.Load(_playbackEvents);
        _nextCacheStart = 0;
        await PrecacheWindowAsync(0, cancellationToken).ConfigureAwait(false);
    }

    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        await _musicTransport.PlayAsync(cancellationToken).ConfigureAwait(false);
        _schedulerLoop.Start(SchedulerLoopAsync, ex => _logger?.LogError(ex, "Error in osu playback scheduler."));
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        await StopSchedulerAsync().ConfigureAwait(false);
        await _musicTransport.PauseAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await StopSchedulerAsync().ConfigureAwait(false);
        await _musicTransport.StopAsync(cancellationToken).ConfigureAwait(false);
        _timelineScheduler.Reset();
        _nextCacheStart = 0;
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        _eventDispatcher.ClearLoops();
        await _musicTransport.SeekAsync(position, cancellationToken).ConfigureAwait(false);
        var eventClock = ToEventClock(position);
        _timelineScheduler.Seek(eventClock);
        await PrecacheWindowAsync((int)eventClock.TotalMilliseconds, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SetPlaybackRateAsync(PlaybackRateState rateState, CancellationToken cancellationToken = default)
    {
        return _musicTransport.SetPlaybackRateAsync(rateState, cancellationToken);
    }

    public async Task SetNightcoreBeatsAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (_options == null || _osuFile == null || _options.EnableNightcoreBeats == enabled)
        {
            return;
        }

        _options.EnableNightcoreBeats = enabled;
        await ReloadPlaybackEventsAsync(Position, cancellationToken).ConfigureAwait(false);
    }

    public void ApplyOptions(OsuAudioSessionOptions options)
    {
        _eventDispatcher.HitsoundVolume = options.HitsoundVolume;
        _eventDispatcher.SampleVolume = options.SampleVolume;
        _eventDispatcher.BalanceFactor = options.BalanceFactor;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await StopSchedulerAsync().ConfigureAwait(false);
        await _musicTransport.ClearAsync(cancellationToken).ConfigureAwait(false);
        _timelineScheduler.Reset();
        _playbackEvents = [];
        _eventBuffer.Clear();
    }

    private async Task StopSchedulerAsync()
    {
        await _schedulerLoop.StopAsync().ConfigureAwait(false);
        _eventDispatcher.ClearLoops();
    }

    public async ValueTask DisposeAsync()
    {
        await ClearAsync().ConfigureAwait(false);
        _eventDispatcher.Dispose();
        await _schedulerLoop.DisposeAsync().ConfigureAwait(false);
    }

    private async Task ReloadPlaybackEventsAsync(TimeSpan seekTo, CancellationToken cancellationToken)
    {
        _playbackEvents = await BuildPlaybackEventsAsync(_osuFile, _options, cancellationToken)
            .ConfigureAwait(false);
        _timelineScheduler.Load(_playbackEvents);
        _timelineScheduler.Seek(ToEventClock(seekTo));
        _nextCacheStart = 0;
        await PrecacheWindowAsync((int)ToEventClock(seekTo).TotalMilliseconds, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<PlaybackEvent>> BuildPlaybackEventsAsync(OsuFile osuFile,
        OsuAudioSessionOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var beatmapSetContext = new BeatmapSetContext(options.Resources.BeatmapFolder);
        await beatmapSetContext.InitializeAsync(
            string.IsNullOrWhiteSpace(options.Resources.BeatmapFilename)
                ? null
                : options.Resources.BeatmapFilename).ConfigureAwait(false);

        var events = await beatmapSetContext.GetHitsoundNodesAsync(osuFile).ConfigureAwait(false);
        if (options.DisableStoryboardSamples)
        {
            events.RemoveAll(static k => k is SampleEvent { Layer: SampleLayer.Sampling });
        }

        if (options.EnableNightcoreBeats)
        {
            events.AddRange(NightcoreBeatGenerator.GetHitsoundNodes(osuFile, Duration));
        }

        return events.OrderBy(static k => k.Offset).ToArray();
    }

    private async Task SchedulerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var position = Position;
            var eventClock = ToEventClock(position);
            await DispatchDueEventsAsync(eventClock, cancellationToken).ConfigureAwait(false);
            StartCacheWindowIfNeeded((int)eventClock.TotalMilliseconds);

            if (Duration > TimeSpan.Zero && position >= Duration)
            {
                _schedulerLoop.Stop();
                _eventDispatcher.ClearLoops();
                Finished?.Invoke();
                break;
            }

            await Task.Delay(GetNextSchedulerDelay(eventClock), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task DispatchDueEventsAsync(TimeSpan eventClock, CancellationToken cancellationToken)
    {
        _eventBuffer.Clear();
        _timelineScheduler.CollectDueEvents(eventClock, _eventBuffer);
        foreach (var playbackEvent in _eventBuffer)
        {
            var cachedAudio = await _eventAudioCache.GetOrCreateAsync(playbackEvent, cancellationToken)
                .ConfigureAwait(false);
            _eventDispatcher.Dispatch(playbackEvent, cachedAudio);
        }
    }

    private TimeSpan GetNextSchedulerDelay(TimeSpan eventClock)
    {
        var nextEventTime = _timelineScheduler.NextEventTime;
        if (nextEventTime == null)
        {
            return MaximumSchedulerDelay;
        }

        var eventDelta = nextEventTime.Value - eventClock;
        if (eventDelta <= TimeSpan.Zero)
        {
            return MinimumSchedulerDelay;
        }

        var rate = Math.Max(Math.Abs(RateState.Rate), 0.01f);
        var realTimeDelay = TimeSpan.FromTicks((long)(eventDelta.Ticks / rate));
        if (realTimeDelay < MinimumSchedulerDelay)
        {
            return MinimumSchedulerDelay;
        }

        return realTimeDelay > MaximumSchedulerDelay
            ? MaximumSchedulerDelay
            : realTimeDelay;
    }

    private void StartCacheWindowIfNeeded(int positionMilliseconds)
    {
        if (positionMilliseconds < _nextCacheStart)
        {
            return;
        }

        var start = _nextCacheStart;
        _nextCacheStart += CacheAdvanceMilliseconds;
        _ = Task.Run(async () =>
        {
            try
            {
                await PrecacheWindowAsync(start).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Precache window abandoned — expected during teardown.
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to precache osu audio window.");
            }
        });
    }

    private Task PrecacheWindowAsync(int startMilliseconds, CancellationToken cancellationToken = default)
    {
        return _eventAudioCache.PrecacheRangeAsync(_playbackEvents,
            startMilliseconds,
            startMilliseconds + CacheWindowMilliseconds,
            cancellationToken);
    }

    private TimeSpan ToEventClock(TimeSpan musicPosition)
    {
        if (_options == null)
        {
            return musicPosition;
        }

        return musicPosition
               - TimeSpan.FromMilliseconds(_options.ManualOffsetMilliseconds)
               + TimeSpan.FromMilliseconds(_options.GeneralOffsetMilliseconds);
    }
}
