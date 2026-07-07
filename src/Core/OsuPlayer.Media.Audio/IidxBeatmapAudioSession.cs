using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IIDXToolbox.Readers.Charts;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using KeyAsio.Core.OsuAudio.Timeline;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using OsuPlayer.Iidx.Abstractions;
using OsuPlayer.Shared.Infrastructure;

namespace OsuPlayer.Media.Audio;

/// <summary>
/// Real-time IIDX playback session. Mirrors <see cref="OsuBeatmapAudioSession"/>'s
/// scheduler/precache architecture but sources audio from decoded 2dx blocks
/// instead of on-disk skin/beatmap files.
/// </summary>
/// <remarks>
/// There is no separate BGM file in IIDX — the chart's <see cref="Chart.Samples"/>
/// (BGM) and <see cref="Chart.Notes"/> (key sounds) are all 2dx blocks. We feed a
/// silent music transport with the chart's total duration, and schedule every
/// block as a <see cref="SampleEvent"/> on the effect bus with the same 12-second
/// sliding precache window the osu! path uses.
/// </remarks>
internal sealed class IidxBeatmapAudioSession : IPlaybackClock, IAsyncDisposable
{
    private static readonly TimeSpan MinimumSchedulerDelay = TimeSpan.FromMilliseconds(1);
    private static readonly TimeSpan MaximumSchedulerDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan LoopWrapDetectionWindow = TimeSpan.FromMilliseconds(250);
    private const int CacheWindowMilliseconds = 12_000;
    private const int CacheAdvanceMilliseconds = 8_000;

    private readonly IPlaybackEngine _playbackEngine;
    private readonly StandaloneMusicTransport _musicTransport;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly IPlaybackRateProcessorFactory _rateProcessorFactory;
    private readonly OsuEffectPlaybackBus _effectBus;
    private readonly IidxPlaybackEventAudioCache _eventAudioCache;
    private readonly PlaybackEventTimelineScheduler _timelineScheduler = new();
    private readonly List<PlaybackEvent> _eventBuffer = new(128);
    private readonly CancellableAsyncLoop _schedulerLoop = new();
    private readonly ILogger? _logger;
    private readonly Lock _timelineGate = new();
    private readonly Lock _precacheGate = new();
    private readonly HashSet<Task> _precacheTasks = new();

    private IReadOnlyList<PlaybackEvent> _playbackEvents = [];
    private IidxLoadedResources _resources = null!;
    private OsuAudioSessionOptions _options = null!;
    private CancellationTokenSource _precacheCts = new();
    private int _nextCacheStart;
    private bool _isLooping;
    private TimeSpan _lastSchedulerPosition;

    public IidxBeatmapAudioSession(
        IPlaybackEngine playbackEngine,
        StandaloneMusicTransport musicTransport,
        AudioCacheManager audioCacheManager,
        IidxLoadedResources resources,
        IPlaybackRateProcessorFactory? rateProcessorFactory = null,
        ILogger? logger = null)
    {
        _playbackEngine = playbackEngine;
        _musicTransport = musicTransport;
        _audioCacheManager = audioCacheManager;
        _rateProcessorFactory = rateProcessorFactory ?? NoPlaybackRateProcessorFactory.Instance;
        _logger = logger;
        _effectBus = new OsuEffectPlaybackBus(playbackEngine.EffectMixer, logger);
        _eventAudioCache = new IidxPlaybackEventAudioCache(audioCacheManager, resources, logger);
    }

    public event Action? Finished;

    public TimeSpan Position => _musicTransport.Position;
    public TimeSpan Duration => _musicTransport.Duration;
    public PlaybackRateState RateState => _musicTransport.RateState;
    public bool IsRunning => _musicTransport.IsRunning;
    public bool SupportsPlaybackRateChange => _musicTransport.SupportsPlaybackRateChange;

    public bool IsLooping
    {
        get => _isLooping;
        set
        {
            _isLooping = value;
            ApplyLoopingToMusicSource();
        }
    }

    public int ManualOffsetMilliseconds
    {
        get => _options?.ManualOffsetMilliseconds ?? 0;
        set => _options.ManualOffsetMilliseconds = value;
    }

    public int GeneralOffsetMilliseconds
    {
        get => _options?.GeneralOffsetMilliseconds ?? 0;
        set => _options.GeneralOffsetMilliseconds = value;
    }

    public float PreservePitchRateCompensationMilliseconds
    {
        get => _options?.PreservePitchRateCompensationMilliseconds
               ?? PlaybackRateState.DefaultPreservePitchCompensationMilliseconds;
        set => _options.PreservePitchRateCompensationMilliseconds = value;
    }

    public async Task LoadAsync(IidxLoadedResources resources, OsuAudioSessionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(options);

        await ClearAsync(cancellationToken).ConfigureAwait(false);

        _resources = resources;
        _options = options;

        var duration = CalculateChartDuration(resources.Chart);
        var musicSource = SilentMusicPlaybackSource.Create(duration,
            _playbackEngine.SourceWaveFormat, _rateProcessorFactory);
        await _musicTransport.LoadAsync(musicSource, ownsSource: true, cancellationToken).ConfigureAwait(false);
        ApplyLoopingToMusicSource();
        _lastSchedulerPosition = TimeSpan.Zero;

        _eventAudioCache.SetContext(resources, _playbackEngine.SourceWaveFormat);
        ApplyOptions(options);

        var playbackEvents = BuildPlaybackEvents(resources.Chart, options);
        lock (_timelineGate)
        {
            _playbackEvents = playbackEvents;
            _timelineScheduler.Load(_playbackEvents);
            _nextCacheStart = CacheAdvanceMilliseconds;
        }

        await PrecacheWindowAsync(playbackEvents, 0, cancellationToken).ConfigureAwait(false);
    }

    public async Task PlayAsync(CancellationToken cancellationToken = default)
    {
        await _musicTransport.PlayAsync(cancellationToken).ConfigureAwait(false);
        StartSchedulerLoop();
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
        lock (_timelineGate)
        {
            _timelineScheduler.Reset();
            _nextCacheStart = 0;
            _lastSchedulerPosition = TimeSpan.Zero;
        }
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        await RunWithSchedulerStoppedAsync(async () =>
        {
            await _musicTransport.SeekAsync(position, cancellationToken).ConfigureAwait(false);
            var eventClock = ToEventClock(position);
            var cacheStart = GetCacheStartMilliseconds(eventClock);
            IReadOnlyList<PlaybackEvent> playbackEvents;
            lock (_timelineGate)
            {
                _timelineScheduler.Seek(eventClock);
                _nextCacheStart = cacheStart + CacheAdvanceMilliseconds;
                _lastSchedulerPosition = position;
                playbackEvents = _playbackEvents;
            }

            await PrecacheWindowAsync(playbackEvents, cacheStart, cancellationToken).ConfigureAwait(false);
        }, clearLoopsWhenStopped: true).ConfigureAwait(false);
    }

    public Task SetPlaybackRateAsync(PlaybackRateState rateState, CancellationToken cancellationToken = default)
    {
        return _musicTransport.SetPlaybackRateAsync(ApplyPlaybackRateOptions(rateState), cancellationToken);
    }

    public void ApplyOptions(OsuAudioSessionOptions options)
    {
        _effectBus.HitsoundVolume = options.HitsoundVolume;
        _effectBus.SampleVolume = options.SampleVolume;
        _effectBus.BalanceFactor = options.BalanceFactor;
        _effectBus.BalanceMode = options.BalanceMode;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await StopSchedulerAsync().ConfigureAwait(false);
        await StopPrecacheAsync().ConfigureAwait(false);
        await _musicTransport.ClearAsync(cancellationToken).ConfigureAwait(false);
        lock (_timelineGate)
        {
            _timelineScheduler.Reset();
            _playbackEvents = [];
            _nextCacheStart = 0;
            _eventBuffer.Clear();
        }

        _effectBus.ClearLoops();
        _eventAudioCache.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await ClearAsync().ConfigureAwait(false);
        _effectBus.Dispose();
        await _schedulerLoop.DisposeAsync().ConfigureAwait(false);
        _precacheCts.Dispose();
    }

    private static List<PlaybackEvent> BuildPlaybackEvents(Chart chart, OsuAudioSessionOptions options)
    {
        var events = new List<PlaybackEvent>(chart.Samples.Count + chart.Notes.Count);

        foreach (var sample in chart.Samples)
        {
            var balance = StereoToBalance(sample.Stereo);
            var evt = PlaybackEvent.Create(
                Guid.NewGuid(),
                sample.Offset,
                1f,
                balance,
                IidxPlaybackEventAudioCache.GetBlockCacheKey(sample.SampleId),
                ResourceOwner.Beatmap,
                SampleLayer.Sampling);
            events.Add(evt);
        }

        foreach (var note in chart.Notes)
        {
            var evt = PlaybackEvent.Create(
                Guid.NewGuid(),
                note.Offset,
                1f,
                LaneToBalance(note.LaneIndex),
                IidxPlaybackEventAudioCache.GetBlockCacheKey(note.SampleChange.SampleId),
                ResourceOwner.Beatmap,
                SampleLayer.Primary);
            events.Add(evt);
        }

        events.Sort(static (a, b) => a.Offset.CompareTo(b.Offset));
        return events;
    }

    private static float StereoToBalance(int stereo)
    {
        if (stereo < 1 || stereo > 15 || stereo == 8) return 0f;
        return (stereo - 8) / 7f;
    }

    private static float LaneToBalance(int laneIndex)
    {
        if (laneIndex < 0) return 0f;
        if (laneIndex > 7) laneIndex -= 8;
        return laneIndex / 3.5f - 1f;
    }

    private static TimeSpan CalculateChartDuration(Chart chart)
    {
        double maxMs = 0;
        foreach (var s in chart.Samples) maxMs = Math.Max(maxMs, s.Offset);
        foreach (var n in chart.Notes) maxMs = Math.Max(maxMs, n.Offset);
        if (maxMs <= 0) maxMs = 1000;
        return TimeSpan.FromMilliseconds(maxMs);
    }

    private async Task StopSchedulerAsync()
    {
        await _schedulerLoop.StopAsync().ConfigureAwait(false);
        _effectBus.ClearLoops();
    }

    private async Task RunWithSchedulerStoppedAsync(Func<Task> work, bool clearLoopsWhenStopped = false)
    {
        var restartScheduler = _schedulerLoop.IsRunning;
        if (restartScheduler)
        {
            await StopSchedulerAsync().ConfigureAwait(false);
        }
        else if (clearLoopsWhenStopped)
        {
            _effectBus.ClearLoops();
        }

        try
        {
            await work().ConfigureAwait(false);
        }
        finally
        {
            if (restartScheduler && IsRunning)
            {
                StartSchedulerLoop();
            }
        }
    }

    private async Task SchedulerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var position = Position;
            if (IsLooping && HasLoopWrapped(position))
            {
                ResetTimelineForLoop();
            }

            var eventClock = ToEventClock(position);
            await DispatchDueEventsAsync(eventClock, cancellationToken).ConfigureAwait(false);
            StartCacheWindowIfNeeded((int)eventClock.TotalMilliseconds);

            if (Duration > TimeSpan.Zero && position >= Duration)
            {
                if (IsLooping)
                {
                    await LoopToStartAsync(cancellationToken).ConfigureAwait(false);
                    _lastSchedulerPosition = Position;
                    continue;
                }

                _schedulerLoop.Stop();
                _effectBus.ClearLoops();
                Finished?.Invoke();
                break;
            }

            _lastSchedulerPosition = position;
            await Task.Delay(GetNextSchedulerDelay(eventClock), cancellationToken).ConfigureAwait(false);
        }
    }

    private bool HasLoopWrapped(TimeSpan position)
    {
        var duration = Duration;
        return duration > TimeSpan.Zero
               && _lastSchedulerPosition + LoopWrapDetectionWindow >= duration
               && position + LoopWrapDetectionWindow < _lastSchedulerPosition;
    }

    private async Task LoopToStartAsync(CancellationToken cancellationToken)
    {
        await _musicTransport.SeekAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
        ResetTimelineForLoop();
    }

    private void ResetTimelineForLoop()
    {
        _effectBus.ClearLoops();
        var loopStartEventClock = ToEventClock(TimeSpan.Zero);
        lock (_timelineGate)
        {
            _timelineScheduler.Seek(loopStartEventClock);
            _nextCacheStart = GetCacheStartMilliseconds(loopStartEventClock) + CacheAdvanceMilliseconds;
        }
    }

    private async Task DispatchDueEventsAsync(TimeSpan eventClock, CancellationToken cancellationToken)
    {
        lock (_timelineGate)
        {
            _eventBuffer.Clear();
            _timelineScheduler.CollectDueEvents(eventClock, _eventBuffer);
        }

        foreach (var playbackEvent in _eventBuffer)
        {
            var cachedAudio = await _eventAudioCache.GetOrCreateAsync(playbackEvent, cancellationToken)
                .ConfigureAwait(false);
            _effectBus.Dispatch(playbackEvent, cachedAudio);
        }
    }

    private TimeSpan GetNextSchedulerDelay(TimeSpan eventClock)
    {
        TimeSpan? nextEventTime;
        lock (_timelineGate)
        {
            nextEventTime = _timelineScheduler.NextEventTime;
        }

        if (nextEventTime == null)
        {
            if (IsLooping && Duration > TimeSpan.Zero)
            {
                var loopEndEventClock = ToEventClock(Duration);
                if (loopEndEventClock > eventClock)
                {
                    return ClampSchedulerDelay(loopEndEventClock - eventClock);
                }
            }

            return MaximumSchedulerDelay;
        }

        var eventDelta = nextEventTime.Value - eventClock;
        if (eventDelta <= TimeSpan.Zero)
        {
            return MinimumSchedulerDelay;
        }

        return ClampSchedulerDelay(eventDelta);
    }

    private TimeSpan ClampSchedulerDelay(TimeSpan eventDelta)
    {
        var rate = Math.Max(Math.Abs(RateState.Rate), 0.01f);
        var realTimeDelay = TimeSpan.FromTicks((long)(eventDelta.Ticks / rate));
        if (realTimeDelay < MinimumSchedulerDelay) return MinimumSchedulerDelay;
        return realTimeDelay > MaximumSchedulerDelay ? MaximumSchedulerDelay : realTimeDelay;
    }

    private void StartCacheWindowIfNeeded(int positionMilliseconds)
    {
        int start;
        IReadOnlyList<PlaybackEvent> events;
        lock (_timelineGate)
        {
            if (positionMilliseconds < _nextCacheStart) return;
            start = _nextCacheStart;
            _nextCacheStart += CacheAdvanceMilliseconds;
            events = _playbackEvents;
        }

        var token = GetPrecacheToken();
        var task = Task.Run(async () =>
        {
            try
            {
                await PrecacheWindowAsync(events, start, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to precache IIDX audio window.");
            }
        });

        TrackPrecacheTask(task);
    }

    private Task PrecacheWindowAsync(IReadOnlyList<PlaybackEvent> events, int startMilliseconds,
        CancellationToken cancellationToken)
    {
        return _eventAudioCache.PrecacheRangeAsync(events, startMilliseconds,
            startMilliseconds + CacheWindowMilliseconds, cancellationToken);
    }

    private CancellationToken GetPrecacheToken()
    {
        lock (_precacheGate) { return _precacheCts.Token; }
    }

    private void TrackPrecacheTask(Task task)
    {
        lock (_precacheGate) { _precacheTasks.Add(task); }
        _ = task.ContinueWith(static (t, s) =>
        {
            var session = (IidxBeatmapAudioSession)s!;
            lock (session._precacheGate) { session._precacheTasks.Remove(t); }
        }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private async Task StopPrecacheAsync()
    {
        CancellationTokenSource cts;
        Task[] tasks;
        lock (_precacheGate)
        {
            cts = _precacheCts;
            _precacheCts = new CancellationTokenSource();
            tasks = _precacheTasks.ToArray();
            _precacheTasks.Clear();
        }

        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        if (tasks.Length > 0)
        {
            try { await Task.WhenAll(tasks).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger?.LogWarning(ex, "Failed while stopping IIDX precache tasks."); }
        }

        cts.Dispose();
    }

    private TimeSpan ToEventClock(TimeSpan musicPosition)
    {
        if (_options == null) return musicPosition;
        return musicPosition
               - TimeSpan.FromMilliseconds(_options.ManualOffsetMilliseconds)
               + TimeSpan.FromMilliseconds(_options.GeneralOffsetMilliseconds);
    }

    private PlaybackRateState ApplyPlaybackRateOptions(PlaybackRateState rateState)
    {
        return _options == null
            ? rateState
            : rateState with
            {
                PreservePitchCompensationMilliseconds = _options.PreservePitchRateCompensationMilliseconds
            };
    }

    private void StartSchedulerLoop()
    {
        _schedulerLoop.Start(SchedulerLoopAsync,
            ex => _logger?.LogError(ex, "Error in IIDX playback scheduler."));
    }

    private void ApplyLoopingToMusicSource()
    {
        if (_musicTransport.Source is { } source) source.IsLooping = _isLooping;
    }

    private static int GetCacheStartMilliseconds(TimeSpan eventClock)
    {
        if (eventClock.TotalMilliseconds <= 0) return 0;
        return eventClock.TotalMilliseconds >= int.MaxValue
            ? int.MaxValue - CacheWindowMilliseconds
            : (int)eventClock.TotalMilliseconds;
    }
}

/// <summary>
/// Caches 2dx audio blocks (memory-resident WAV bytes) into the KeyAsio
/// <see cref="AudioCacheManager"/> keyed by block index, and serves them
/// to the IIDX playback scheduler.
/// </summary>
internal sealed class IidxPlaybackEventAudioCache
{
    private readonly AudioCacheManager _audioCacheManager;
    private readonly IidxLoadedResources _resources;
    private readonly ILogger? _logger;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, CachedAudio?> _blockCache = new(StringComparer.Ordinal);

    private WaveFormat _waveFormat = null!;
    private int _contextVersion;

    public IidxPlaybackEventAudioCache(AudioCacheManager audioCacheManager,
        IidxLoadedResources resources, ILogger? logger)
    {
        _audioCacheManager = audioCacheManager;
        _resources = resources;
        _logger = logger;
    }

    public static string GetBlockCacheKey(int sampleId) => $"iidx-block-{sampleId}";

    public void SetContext(IidxLoadedResources resources, WaveFormat waveFormat)
    {
        lock (_gate)
        {
            _contextVersion++;
            _blockCache.Clear();
            _waveFormat = waveFormat;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _contextVersion++;
            _blockCache.Clear();
        }
    }

    public async Task<CachedAudio?> GetOrCreateAsync(PlaybackEvent playbackEvent,
        CancellationToken cancellationToken = default)
    {
        var key = playbackEvent.Filename!;
        int version;
        lock (_gate)
        {
            if (_blockCache.TryGetValue(key, out var cached)) return cached;
            version = _contextVersion;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadBlockAsync(key, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            if (version != _contextVersion)
            {
                throw new OperationCanceledException("IIDX audio cache context changed.");
            }

            _blockCache[key] = loaded;
        }

        return loaded;
    }

    public Task PrecacheRangeAsync(IEnumerable<PlaybackEvent> events, double startMs, double endMs,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            foreach (var evt in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (evt.Offset < startMs || evt.Offset >= endMs) continue;
                _ = await GetOrCreateAsync(evt, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);
    }

    private async Task<CachedAudio?> LoadBlockAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var sampleId = ExtractSampleId(cacheKey);
        if (sampleId <= 0 || sampleId > _resources.AudioBlocks.Count) return null;

        var block = _resources.AudioBlocks[sampleId - 1];
        if (block.Length == 0) return null;

        await using var stream = new MemoryStream(block.ToArray());
        var (cachedAudio, status) = await _audioCacheManager
            .GetOrCreateOrEmptyAsync(cacheKey, stream, _waveFormat, "iidx")
            .ConfigureAwait(false);

        if (status == CacheGetStatus.Failed)
        {
            _logger?.LogWarning("Failed to cache IIDX block: {Key}", cacheKey);
            return null;
        }

        return cachedAudio;
    }

    private static int ExtractSampleId(string cacheKey)
    {
        const string prefix = "iidx-block-";
        return int.TryParse(cacheKey.AsSpan(prefix.Length), out var id) ? id : 0;
    }
}