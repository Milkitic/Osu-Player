using System;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;

namespace Milky.OsuPlayer.Media.Audio.Coordination;

/// <summary>
/// Centralises the publication of controller-level events. All events are
/// raised on the UI thread; subscribers therefore never have to marshal
/// themselves.
/// </summary>
/// <remarks>
/// Replaces the nine loose <c>event</c> fields previously declared on
/// <see cref="ObservablePlayController"/> plus their seven
/// <c>_uiThreadDispatcher.Send(() =&gt; ...)</c> call-sites. Subscribing
/// through this bus is a drop-in replacement for the old event surface.
/// </remarks>
internal sealed class PlayerEventBus
{
    private readonly IUiThreadDispatcher _dispatcher;
    private readonly NLog.Logger _logger;

    public PlayerEventBus(IUiThreadDispatcher dispatcher, NLog.Logger logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

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

    public void RaisePlayStatusChanged(PlayStatus status)
        => _dispatcher.Post(() => PlayStatusChanged?.Invoke(status));

    public void RaisePositionUpdated(TimeSpan position)
        => _dispatcher.Post(() => PositionUpdated?.Invoke(position));

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
                _logger.Error(ex, "Error while raising position-set request.");
            }
        }
    }

    public void RaiseInterfaceClearRequest()
        => _dispatcher.Send(() => InterfaceClearRequest?.Invoke());

    public void RaisePreLoadStarted(string path, CancellationToken token)
        => _dispatcher.Send(() => PreLoadStarted?.Invoke(path, token));

    public void RaiseLoadStarted(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => LoadStarted?.Invoke(context, token));

    public void RaiseMetaLoaded(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => MetaLoaded?.Invoke(context, token));

    public void RaiseBackgroundInfoLoaded(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => BackgroundInfoLoaded?.Invoke(context, token));

    public void RaiseMusicLoaded(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => MusicLoaded?.Invoke(context, token));

    public void RaiseVideoLoadRequested(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => VideoLoadRequested?.Invoke(context, token));

    public void RaiseStoryboardLoadRequested(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => StoryboardLoadRequested?.Invoke(context, token));

    public void RaiseLoadFinished(BeatmapContext context, CancellationToken token)
        => _dispatcher.Send(() => LoadFinished?.Invoke(context, token));

    public void RaiseLoadError(BeatmapContext? context, Exception ex)
    {
        // The event signature is non-nullable for compatibility with
        // assemblies that don't enable nullable reference types; the few
        // call sites that pass null (e.g. when the playlist is empty) log
        // here and skip the broadcast instead of synthesising a placeholder.
        if (context == null)
        {
            _logger.Error(ex, "Load error with no current beatmap context.");
            return;
        }
        _dispatcher.Send(() => LoadError?.Invoke(context, ex));
    }
}
