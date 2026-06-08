using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuPlayer.Media.Audio.Playlist;
using OsuPlayer.Shared;

namespace OsuPlayer.Media.Audio.Coordination;

/// <summary>
/// Centralises the publication of controller-level events. All events are
/// raised on the UI thread; subscribers therefore never have to marshal
/// themselves.
/// </summary>
public sealed class PlayerEventBus : IDisposable
{
    private readonly IUiThreadDispatcher _dispatcher;
    private readonly ILogger<PlayerEventBus> _logger;
    private OsuMixPlayer? _player;

    public PlayerEventBus(
        IUiThreadDispatcher dispatcher,
        ILogger<PlayerEventBus> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public event Action<PlayStatus>? PlayStatusChanged;
    public event Action<TimeSpan>? PositionUpdated;
    public event Action? PlayerChanged;
    public event Func<BeatmapContext, double, bool, Task>? PositionSetRequested;
    public event Action? InterfaceClearRequest;
    public event Action<Exception>? AudioDeviceError;
    public event Action<string, CancellationToken>? PreLoadStarted;
    public event Action<BeatmapContext, CancellationToken>? LoadStarted;
    public event Action<BeatmapContext, CancellationToken>? MetaLoaded;
    public event Action<BeatmapContext, CancellationToken>? BackgroundInfoLoaded;
    public event Action<BeatmapContext, CancellationToken>? MusicLoaded;
    public event Action<BeatmapContext, CancellationToken>? VideoLoadRequested;
    public event Action<BeatmapContext, CancellationToken>? StoryboardLoadRequested;
    public event Action<BeatmapContext, CancellationToken>? LoadFinished;
    public event Action<BeatmapContext, Exception>? LoadError;

    public OsuMixPlayer? Player => _player;
    public bool IsPlayerReady => _player != null && _player.PlayStatus != PlayStatus.Unknown;

    public void AttachPlayer(OsuMixPlayer player)
    {
        if (_player != null)
        {
            DetachPlayer();
        }

        _player = player;
        _player.PlayStatusChanged += OnPlayerPlayStatusChanged;
        _player.PositionUpdated += OnPlayerPositionUpdated;
        PlayerChanged?.Invoke();

        if (player.PlayStatus != PlayStatus.Unknown)
        {
            OnPlayerPlayStatusChanged(player.PlayStatus);
        }
    }

    public void DetachPlayer()
    {
        var existing = _player;
        if (existing == null) return;

        _player = null;
        existing.PlayStatusChanged -= OnPlayerPlayStatusChanged;
        existing.PositionUpdated -= OnPlayerPositionUpdated;
        PlayerChanged?.Invoke();
    }

    public void OnPlaybackEngineDeviceError(Exception ex)
    {
        _logger.LogError(ex, "Audio device error.");
        _dispatcher.Post(() => AudioDeviceError?.Invoke(ex));
    }

    public void Dispose() => DetachPlayer();

    public void RaiseInterfaceClearRequest() => Send(InterfaceClearRequest);
    public void RaisePreLoadStarted(string path, CancellationToken token) => Send(PreLoadStarted, path, token);
    public void RaiseLoadStarted(BeatmapContext ctx, CancellationToken token) => Send(LoadStarted, ctx, token);
    public void RaiseMetaLoaded(BeatmapContext ctx, CancellationToken token) => Send(MetaLoaded, ctx, token);
    public void RaiseBackgroundInfoLoaded(BeatmapContext ctx, CancellationToken token) => Send(BackgroundInfoLoaded, ctx, token);
    public void RaiseMusicLoaded(BeatmapContext ctx, CancellationToken token) => Send(MusicLoaded, ctx, token);
    public void RaiseVideoLoadRequested(BeatmapContext ctx, CancellationToken token) => Send(VideoLoadRequested, ctx, token);
    public void RaiseStoryboardLoadRequested(BeatmapContext ctx, CancellationToken token) => Send(StoryboardLoadRequested, ctx, token);
    public void RaiseLoadFinished(BeatmapContext ctx, CancellationToken token) => Send(LoadFinished, ctx, token);

    public void RaiseLoadError(BeatmapContext? context, Exception ex)
    {
        if (context == null)
        {
            _logger.LogError(ex, "Load error with no current beatmap context.");
            return;
        }
        Send(LoadError, context, ex);
    }

    public async Task RaisePositionSetRequestedAsync(BeatmapContext context, double time, bool play)
    {
        var handlers = PositionSetRequested?.GetInvocationList();
        if (handlers == null) return;

        foreach (Func<BeatmapContext, double, bool, Task> handler in handlers)
        {
            try
            {
                await handler(context, time, play).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while raising position-set request.");
            }
        }
    }

    private void Send(Action? handler) => _dispatcher.Send(() => handler?.Invoke());
    private void Send<T>(Action<T>? handler, T arg) => _dispatcher.Send(() => handler?.Invoke(arg));
    private void Send<T1, T2>(Action<T1, T2>? handler, T1 a1, T2 a2) => _dispatcher.Send(() => handler?.Invoke(a1, a2));
    private void Post<T>(Action<T>? handler, T arg) => _dispatcher.Post(() => handler?.Invoke(arg));

    private void OnPlayerPlayStatusChanged(PlayStatus status)
        => Post(PlayStatusChanged, status);

    private void OnPlayerPositionUpdated(TimeSpan position)
        => Post(PositionUpdated, position);
}
