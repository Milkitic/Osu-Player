using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Coosu.Beatmap.MetaData;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio.Coordination;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Media.Audio;

/// <summary>
/// Top-level facade that the UI binds to. Delegates all real work to
/// <see cref="PlayerEventBus"/>, <see cref="PlayerSessionService"/>, and
/// <see cref="PlayerStatePump"/>; this class only wires them together and
/// surfaces the historical public surface so existing consumers keep
/// compiling.
/// </summary>
public sealed partial class ObservablePlayController : ObservableObject, IPlaybackController, IAsyncDisposable
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IPlaybackEngine _playbackEngine;
    private readonly PlayerEventBus _bus;
    private readonly PlayerStatePump _pump;
    private readonly PlayerSessionService _session;

    private static readonly NullPlaybackController s_nullController = new();

    public ObservablePlayController(
        IPlayerDataStore playerData,
        IPlaybackEngine playbackEngine,
        IAudioDeviceManager audioDeviceManager,
        AudioCacheManager audioCacheManager,
        Action<Exception> audioDeviceErrorHandler,
        IUiThreadDispatcher uiThreadDispatcher)
    {
        _playbackEngine = playbackEngine;

        _bus = new PlayerEventBus(uiThreadDispatcher, s_logger);
        _pump = new PlayerStatePump(_bus, audioDeviceErrorHandler, uiThreadDispatcher, s_logger);
        _pump.PlayStatusChanged += OnPumpPlayStatusChanged;
        _pump.PositionUpdated += position => PositionUpdated?.Invoke(position);
        _pump.PlayerChanged += OnPumpPlayerChanged;

        var beatmapLoader = new BeatmapLoader(playerData);
        var loadService = new BeatmapLoadService(beatmapLoader);
        PlayList = new PlayList(playerData, uiThreadDispatcher, OnSongListChanged, OnModeChanged);
        _session = new PlayerSessionService(
            _bus,
            _pump,
            PlayList,
            beatmapLoader,
            loadService,
            new SemaphoreSlim(1, 1),
            playerData,
            playbackEngine,
            audioCacheManager,
            s_logger);

        _playbackEngine.DeviceError += _pump.OnPlaybackEngineDeviceError;
        PlayList.AutoSwitched += _session.HandleAutoSwitchedAsync;

        // Re-export bus events onto the facade surface for legacy subscribers.
        _bus.PreLoadStarted += (path, ct) => PreLoadStarted?.Invoke(path, ct);
        _bus.LoadStarted += (ctx, ct) => LoadStarted?.Invoke(ctx, ct);
        _bus.MetaLoaded += (ctx, ct) => MetaLoaded?.Invoke(ctx, ct);
        _bus.BackgroundInfoLoaded += (ctx, ct) => BackgroundInfoLoaded?.Invoke(ctx, ct);
        _bus.MusicLoaded += (ctx, ct) => MusicLoaded?.Invoke(ctx, ct);
        _bus.VideoLoadRequested += (ctx, ct) => VideoLoadRequested?.Invoke(ctx, ct);
        _bus.StoryboardLoadRequested += (ctx, ct) => StoryboardLoadRequested?.Invoke(ctx, ct);
        _bus.LoadFinished += (ctx, ct) => LoadFinished?.Invoke(ctx, ct);
        _bus.LoadError += OnLoadErrorForwarded;
        _bus.InterfaceClearRequest += () => InterfaceClearRequest?.Invoke();
        _bus.PositionSetRequested += (ctx, time, play) =>
        {
            var handler = PositionSetRequested;
            return handler == null ? Task.CompletedTask : handler(ctx, time, play);
        };
    }

    public PlayList PlayList { get; }

    public OsuMixPlayer? Player => _pump.Player;

    public bool IsPlayerReady => _pump.IsPlayerReady;

    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;
    public event Func<BeatmapContext, double, bool, Task>? PositionSetRequested;
    public event Action? InterfaceClearRequest;
    public event Action<string, CancellationToken>? PreLoadStarted;
    public event Action<BeatmapContext, CancellationToken>? LoadStarted;
    public event Action<BeatmapContext, CancellationToken>? MetaLoaded;
    public event Action<BeatmapContext, CancellationToken>? BackgroundInfoLoaded;
    public event Action<BeatmapContext, CancellationToken>? MusicLoaded;
    public event Action<BeatmapContext, CancellationToken>? VideoLoadRequested;
    public event Action<BeatmapContext, CancellationToken>? StoryboardLoadRequested;
    public event Action<BeatmapContext, CancellationToken>? LoadFinished;
    public event Action<BeatmapContext, Exception>? LoadError;

    public Task PlayAsync() => ActiveController().PlayAsync();
    public Task PauseAsync() => ActiveController().PauseAsync();
    public Task StopAsync() => ActiveController().StopAsync();
    public Task RestartAsync() => ActiveController().RestartAsync();
    public Task TogglePlayAsync() => ActiveController().TogglePlayAsync();

    public async Task SetTimeAsync(double time, bool play)
    {
        await ActiveController().SetTimeAsync(time, play).ConfigureAwait(false);
        if (PlayList.CurrentInfo != null)
        {
            await _bus.RaisePositionSetRequestedAsync(PlayList.CurrentInfo, time, play).ConfigureAwait(false);
        }
    }

    public Task PlayNewAsync(Beatmap? beatmap, bool playInstantly = true)
    {
        if (beatmap is null) return Task.CompletedTask;
        return _session.PlayNewFromBeatmapAsync(beatmap, playInstantly);
    }

    public Task PlayNewAsync(string path, bool playInstantly = true)
        => _session.PlayNewFromPathAsync(path, playInstantly);

    public Task PlayPrevAsync() => _session.PlayByControlAsync(PlayControlType.Previous);
    public Task PlayNextAsync() => _session.PlayByControlAsync(PlayControlType.Next);

    public async ValueTask DisposeAsync()
    {
        _playbackEngine.DeviceError -= _pump.OnPlaybackEngineDeviceError;
        _pump.PlayStatusChanged -= OnPumpPlayStatusChanged;
        _pump.PlayerChanged -= OnPumpPlayerChanged;
        PlayList.AutoSwitched -= _session.HandleAutoSwitchedAsync;
        await _session.DisposeAsync().ConfigureAwait(false);
        _pump.Dispose();
    }

    private IPlaybackController ActiveController()
        => _pump.IsPlayerReady ? _pump.Player! : s_nullController;

    private void OnPumpPlayStatusChanged(PlayStatus status)
    {
        SharedVm.Default.IsPlaying = status == PlayStatus.Playing;
        OnPropertyChanged(nameof(IsPlayerReady));
        PlayStatusChanged?.Invoke(status);
    }

    private void OnPumpPlayerChanged()
    {
        OnPropertyChanged(nameof(Player));
        OnPropertyChanged(nameof(IsPlayerReady));
    }

    private void OnSongListChanged()
    {
        AppSettings.Default.CurrentList = new HashSet<MapIdentity>(PlayList.SongList.Select(k => k.GetIdentity()));
        AppSettings.SaveDefault();
    }

    private void OnModeChanged(PlaylistMode mode)
    {
        AppSettings.Default.Play.PlayListMode = mode;
        AppSettings.SaveDefault();
    }

    private void OnLoadErrorForwarded(BeatmapContext ctx, Exception ex)
    {
        if (ctx.BeatmapDetail != null)
        {
            s_logger.Error(ex, "Load error while loading beatmap: {0}",
                Path.Combine(ctx.BeatmapDetail.BaseFolder ?? "", ctx.BeatmapDetail.MapPath ?? ""));
        }
        else
        {
            s_logger.Error(ex, "Load error while loading beatmap.");
        }
        LoadError?.Invoke(ctx, ex);
    }

    private sealed class NullPlaybackController : IPlaybackController
    {
        public Task PlayAsync() => Task.CompletedTask;
        public Task PauseAsync() => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;
        public Task RestartAsync() => Task.CompletedTask;
        public Task TogglePlayAsync() => Task.CompletedTask;
        public Task SetTimeAsync(double time, bool play) => Task.CompletedTask;
    }
}
