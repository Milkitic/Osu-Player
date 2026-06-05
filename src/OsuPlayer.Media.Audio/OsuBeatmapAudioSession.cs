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
    private readonly Lock _timelineGate = new();
    private readonly Lock _precacheGate = new();
    private readonly HashSet<Task> _precacheTasks = new();

    private IReadOnlyList<PlaybackEvent> _playbackEvents = [];
    private OsuFile _osuFile = null!;
    private OsuAudioSessionOptions _options = null!;
    private CancellationTokenSource _precacheCts = new();
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

        var playbackEvents = await BuildPlaybackEventsAsync(osuFile, options, cancellationToken).ConfigureAwait(false);
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
        }
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken cancellationToken = default)
    {
        var wasRunning = _schedulerLoop.IsRunning;
        if (wasRunning)
        {
            await StopSchedulerAsync().ConfigureAwait(false);
        }
        else
        {
            _eventDispatcher.ClearLoops();
        }

        try
        {
            await _musicTransport.SeekAsync(position, cancellationToken).ConfigureAwait(false);
            var eventClock = ToEventClock(position);
            var cacheStart = GetCacheStartMilliseconds(eventClock);
            IReadOnlyList<PlaybackEvent> playbackEvents;
            lock (_timelineGate)
            {
                _timelineScheduler.Seek(eventClock);
                _nextCacheStart = cacheStart + CacheAdvanceMilliseconds;
                playbackEvents = _playbackEvents;
            }

            await PrecacheWindowAsync(playbackEvents, cacheStart, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (wasRunning && IsRunning)
            {
                StartSchedulerLoop();
            }
        }
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
        await ReloadPlaybackEventsAsync(cancellationToken).ConfigureAwait(false);
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
        await StopPrecacheAsync().ConfigureAwait(false);
        await _musicTransport.ClearAsync(cancellationToken).ConfigureAwait(false);
        lock (_timelineGate)
        {
            _timelineScheduler.Reset();
            _playbackEvents = [];
            _nextCacheStart = 0;
            _eventBuffer.Clear();
        }

        _eventAudioCache.Clear();
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
        _precacheCts.Dispose();
    }

    private async Task ReloadPlaybackEventsAsync(CancellationToken cancellationToken)
    {
        var restartScheduler = _schedulerLoop.IsRunning;
        if (restartScheduler)
        {
            await StopSchedulerAsync().ConfigureAwait(false);
        }

        try
        {
            await StopPrecacheAsync().ConfigureAwait(false);
            var playbackEvents = await BuildPlaybackEventsAsync(_osuFile, _options, cancellationToken)
                .ConfigureAwait(false);
            var eventClock = ToEventClock(Position);
            var cacheStart = GetCacheStartMilliseconds(eventClock);

            lock (_timelineGate)
            {
                _playbackEvents = playbackEvents;
                _timelineScheduler.Load(_playbackEvents);
                _timelineScheduler.Seek(eventClock);
                _nextCacheStart = cacheStart + CacheAdvanceMilliseconds;
            }

            await PrecacheWindowAsync(playbackEvents, cacheStart, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (restartScheduler && IsRunning)
            {
                StartSchedulerLoop();
            }
        }
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
        lock (_timelineGate)
        {
            _eventBuffer.Clear();
            _timelineScheduler.CollectDueEvents(eventClock, _eventBuffer);
        }

        foreach (var playbackEvent in _eventBuffer)
        {
            var cachedAudio = await _eventAudioCache.GetOrCreateAsync(playbackEvent, cancellationToken)
                .ConfigureAwait(false);
            _eventDispatcher.Dispatch(playbackEvent, cachedAudio);
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
        int start;
        IReadOnlyList<PlaybackEvent> events;
        lock (_timelineGate)
        {
            if (positionMilliseconds < _nextCacheStart)
            {
                return;
            }

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
            catch (OperationCanceledException)
            {
                // Precache window abandoned — expected during teardown.
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to precache osu audio window.");
            }
        });

        TrackPrecacheTask(task);
    }

    private Task PrecacheWindowAsync(int startMilliseconds, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlaybackEvent> playbackEvents;
        lock (_timelineGate)
        {
            playbackEvents = _playbackEvents;
        }

        return PrecacheWindowAsync(playbackEvents, startMilliseconds, cancellationToken);
    }

    private Task PrecacheWindowAsync(
        IReadOnlyList<PlaybackEvent> playbackEvents,
        int startMilliseconds,
        CancellationToken cancellationToken)
    {
        return _eventAudioCache.PrecacheRangeAsync(playbackEvents,
            startMilliseconds,
            startMilliseconds + CacheWindowMilliseconds,
            cancellationToken);
    }

    private CancellationToken GetPrecacheToken()
    {
        lock (_precacheGate)
        {
            return _precacheCts.Token;
        }
    }

    private void TrackPrecacheTask(Task task)
    {
        lock (_precacheGate)
        {
            _precacheTasks.Add(task);
        }

        _ = task.ContinueWith(static (completedTask, state) =>
        {
            var session = (OsuBeatmapAudioSession)state!;
            lock (session._precacheGate)
            {
                session._precacheTasks.Remove(completedTask);
            }
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

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Shutdown can race with a previous cancellation path.
        }

        if (tasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when abandoning an in-flight precache window.
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed while stopping osu audio precache tasks.");
            }
        }

        cts.Dispose();
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

    private void StartSchedulerLoop()
    {
        _schedulerLoop.Start(SchedulerLoopAsync, ex => _logger?.LogError(ex, "Error in osu playback scheduler."));
    }

    private static int GetCacheStartMilliseconds(TimeSpan eventClock)
    {
        if (eventClock.TotalMilliseconds <= 0)
        {
            return 0;
        }

        return eventClock.TotalMilliseconds >= int.MaxValue
            ? int.MaxValue - CacheWindowMilliseconds
            : (int)eventClock.TotalMilliseconds;
    }
}
