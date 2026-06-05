using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Media.Audio.Infrastructure;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Media.Audio.Coordination;

/// <summary>
/// Coordinates beatmap selection, loading, and playback commands for the
/// controller facade.
/// </summary>
/// <remarks>
/// This service owns the session operation token: user-initiated loads and
/// manual previous/next commands cancel superseded work, while auto-advance
/// continues the current operation. It serializes load/clear work with the
/// supplied <see cref="SemaphoreSlim"/>, attaches and detaches the active
/// <see cref="OsuMixPlayer"/> through <see cref="PlayerStatePump"/>, and
/// translates <see cref="PlayControlResult"/> values into player commands.
/// </remarks>
internal sealed class PlayerSessionService : IAsyncDisposable
{
    private readonly PlayerEventBus _bus;
    private readonly PlayerStatePump _pump;
    private readonly PlayList _playList;
    private readonly BeatmapLoader _beatmapLoader;
    private readonly BeatmapLoadService _loadService;
    private readonly SemaphoreSlim _readLock;
    private readonly IPlayerDataStore _playerData;
    private readonly IPlaybackEngine _playbackEngine;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly NLog.Logger _logger;
    private readonly Lock _operationGate = new();

    private CancellationTokenSource _cts = new();
    private bool _isHandlingLoadFailure;
    private int _isHandlingPlaybackFinished;

    public PlayerSessionService(
        PlayerEventBus bus,
        PlayerStatePump pump,
        PlayList playList,
        BeatmapLoader beatmapLoader,
        BeatmapLoadService loadService,
        SemaphoreSlim readLock,
        IPlayerDataStore playerData,
        IPlaybackEngine playbackEngine,
        AudioCacheManager audioCacheManager,
        NLog.Logger logger)
    {
        _bus = bus;
        _pump = pump;
        _playList = playList;
        _beatmapLoader = beatmapLoader;
        _loadService = loadService;
        _readLock = readLock;
        _playerData = playerData;
        _playbackEngine = playbackEngine;
        _audioCacheManager = audioCacheManager;
        _logger = logger;

        _pump.PlayStatusChanged += OnPlayerPlayStatusChanged;
    }

    public Task PlayNewFromBeatmapAsync(Milky.OsuPlayer.Data.Models.Beatmap beatmap, bool playInstantly)
    {
        var operationToken = InterruptPrevOperation();
        return LoadAndPlayAsync(async token =>
        {
            token.ThrowIfCancellationRequested();
            await _playList.AddOrSwitchToAsync(beatmap).ConfigureAwait(false);
        }, playInstantly, operationToken);
    }

    public async Task PlayNewFromPathAsync(string path, bool playInstantly)
    {
        var operationToken = InterruptPrevOperation();
        BeatmapContext? contextToUpdate = null;
        LoadScope? scope = null;
        try
        {
            scope = await LoadScope.AcquireAsync(_readLock, operationToken).ConfigureAwait(false);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Cannot locate file", path);
            }

            if (_playList.CurrentInfo is null)
            {
                _playList.InitializeEmptyCurrentInfo();
            }

            var setupContext = _playList.CurrentInfo!;
            setupContext.BeatmapDetail.MapPath = path;
            setupContext.BeatmapDetail.BaseFolder = Path.GetDirectoryName(path) ?? string.Empty;

            await ClearPlayerAsync().ConfigureAwait(false);
            _bus.RaisePreLoadStarted(path, operationToken);

            var osuFile = await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor"))
                .ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();
            setupContext.OsuFile = osuFile;

            var loadResult = await _beatmapLoader.LoadFromOsuFileAsync(
                osuFile, path, setupContext.BeatmapSettings, operationToken).ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();

            var trueBeatmap = loadResult.Beatmap;
            await _playList.AddOrSwitchToAsync(trueBeatmap).ConfigureAwait(false);

            var newContext = _playList.CurrentInfo
                ?? throw new InvalidOperationException("Playlist did not create a current beatmap context.");
            BeatmapLoadService.ApplyToContext(newContext, loadResult, operationToken);
            contextToUpdate = newContext;

            _bus.RaiseLoadStarted(newContext, operationToken);
            await FinishLoadAsync(newContext, loadResult, operationToken).ConfigureAwait(false);

            if (!operationToken.IsCancellationRequested && playInstantly && _pump.Player != null)
            {
                await _pump.Player.PlayAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
        {
            // Superseded by a newer playback operation.
        }
        catch (Exception ex)
        {
            HandleLoadFailure(_playList.CurrentInfo, ex);
        }
        finally
        {
            scope?.Dispose();
            if (contextToUpdate is not null && !operationToken.IsCancellationRequested)
            {
                await _playerData.TryUpdateMapAsync(contextToUpdate.Beatmap.GetIdentity()).ConfigureAwait(false);
            }
        }
    }

    public Task PlayByControlAsync(PlayControlType control) => PlayByControlAsync(control, autoAdvance: false);

    public async Task PlayByControlAsync(PlayControlType control, bool autoAdvance)
    {
        CancellationToken operationToken = default;
        try
        {
            operationToken = autoAdvance
                ? GetCurrentOperationToken()
                : InterruptPrevOperation();

            if (!autoAdvance)
            {
                operationToken.ThrowIfCancellationRequested();
            }

            var preInfo = _playList.CurrentInfo;
            var controlResult = autoAdvance
                ? await _playList.InvokeAutoNext().ConfigureAwait(false)
                : await _playList.SwitchByControl(control).ConfigureAwait(false);

            await ApplyPlayControlResultAsync(controlResult, preInfo, playInstantly: true, operationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
        {
            // Superseded by a newer playback operation.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while changing song.");
        }
    }

    public async Task HandleAutoSwitchedAsync(PlayControlResult controlResult, Milky.OsuPlayer.Data.Models.Beatmap? beatmap, bool playInstantly)
    {
        var operationToken = GetCurrentOperationToken();
        try
        {
            await ApplyPlayControlResultAsync(controlResult, _playList.PreInfo, playInstantly, operationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
        {
            // Superseded by a newer playback operation.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while auto changing song.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _pump.PlayStatusChanged -= OnPlayerPlayStatusChanged;
        CancellationTokenSource cts;
        lock (_operationGate)
        {
            cts = _cts;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by a racing shutdown path.
        }

        await ClearPlayerAsync().ConfigureAwait(false);
        _readLock.Dispose();
        cts.Dispose();
    }

    private async Task LoadAndPlayAsync(
        Func<CancellationToken, Task> setup,
        bool playInstantly,
        CancellationToken operationToken,
        bool ownsLock = false)
    {
        LoadScope? scope = null;
        try
        {
            if (ownsLock)
            {
                scope = await LoadScope.AcquireAsync(_readLock, operationToken).ConfigureAwait(false);
            }

            await setup(operationToken).ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            HandleLoadFailure(_playList.CurrentInfo, ex);
            return;
        }
        finally
        {
            scope?.Dispose();
        }

        if (playInstantly)
        {
            var loaded = await LoadCoreAsync(lockAlreadyHeld: false, operationToken).ConfigureAwait(false);
            if (!operationToken.IsCancellationRequested && loaded && _pump.Player != null)
            {
                await _pump.Player.PlayAsync().ConfigureAwait(false);
            }
        }
        else
        {
            await LoadCoreAsync(lockAlreadyHeld: false, operationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> LoadCoreAsync(bool lockAlreadyHeld, CancellationToken operationToken)
    {
        var context = _playList.CurrentInfo;
        if (context == null)
        {
            return false;
        }

        LoadScope? scope = null;
        var loaded = false;
        var shouldTryFallback = false;

        try
        {
            if (!lockAlreadyHeld)
            {
                scope = await LoadScope.AcquireAsync(_readLock, operationToken).ConfigureAwait(false);
                await ClearPlayerAsync().ConfigureAwait(false);
            }

            operationToken.ThrowIfCancellationRequested();
            _bus.RaiseLoadStarted(context, operationToken);

            BeatmapLoadResult loadResult = context.OsuFile is null
                ? await _loadService.LoadFromBeatmapAsync(context, operationToken).ConfigureAwait(false)
                : await _loadService.LoadFromOsuFileAsync(context, context.BeatmapDetail.MapPath, operationToken)
                    .ConfigureAwait(false);

            await FinishLoadAsync(context, loadResult, operationToken).ConfigureAwait(false);
            loaded = true;
        }
        catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            HandleLoadFailure(_playList.CurrentInfo, ex);
            shouldTryFallback = !lockAlreadyHeld
                                && !_isHandlingLoadFailure
                                && _pump.Player?.PlayStatus != PlayStatus.Playing;
        }
        finally
        {
            scope?.Dispose();
            if (!operationToken.IsCancellationRequested)
            {
                await _playerData.TryUpdateMapAsync(context.Beatmap.GetIdentity()).ConfigureAwait(false);
            }
        }

        if (shouldTryFallback)
        {
            _isHandlingLoadFailure = true;
            try
            {
                await PlayByControlAsync(PlayControlType.Next).ConfigureAwait(false);
            }
            finally
            {
                _isHandlingLoadFailure = false;
            }
        }

        return loaded;
    }

    private async Task FinishLoadAsync(
        BeatmapContext context,
        BeatmapLoadResult loadResult,
        CancellationToken operationToken)
    {
        operationToken.ThrowIfCancellationRequested();
        _bus.RaiseMetaLoaded(context, operationToken);
        _bus.RaiseBackgroundInfoLoaded(context, operationToken);

        var player = new OsuMixPlayer(loadResult.OsuFile, loadResult.BaseFolder, _playbackEngine, _audioCacheManager);
        var attached = false;
        try
        {
            await player.Initialize().ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();
            player.ManualOffset = context.BeatmapSettings?.Offset ?? 0;
            _pump.AttachPlayer(player);
            attached = true;
            operationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            if (attached && ReferenceEquals(_pump.Player, player))
            {
                _pump.DetachPlayer();
            }

            await SafeStopExtensions.TryDisposeAsync(
                async () => await player.DisposeAsync().ConfigureAwait(false),
                _logger,
                "Error while disposing failed player initialization.").ConfigureAwait(false);
            throw;
        }

        _bus.RaiseMusicLoaded(context, operationToken);

        if (loadResult.VideoPath != null)
        {
            context.BeatmapDetail.VideoPath = loadResult.VideoPath;
            _bus.RaiseVideoLoadRequested(context, operationToken);
        }

        if (loadResult.HasStoryboard)
        {
            _bus.RaiseStoryboardLoadRequested(context, operationToken);
        }

        context.FullLoaded = true;
        _bus.RaiseLoadFinished(context, operationToken);
        AppSettings.Default.CurrentMap = context.Beatmap.GetIdentity();
        AppSettings.SaveDefault();
    }

    private void HandleLoadFailure(BeatmapContext? context, Exception ex)
    {
        _bus.RaiseLoadError(context, ex);
        _logger.Error(ex, "Error while loading new beatmap. BeatmapId: {0}; BeatmapSetId: {1}",
            context?.Beatmap?.BeatmapId, context?.Beatmap?.BeatmapSetId);
    }

    private async Task ApplyPlayControlResultAsync(
        PlayControlResult controlResult,
        BeatmapContext? previousContext,
        bool playInstantly,
        CancellationToken operationToken)
    {
        operationToken.ThrowIfCancellationRequested();
        var context = _playList.CurrentInfo;

        if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear || context is null)
        {
            await ClearPlayerAsync().ConfigureAwait(false);
            _bus.RaiseInterfaceClearRequest();
            return;
        }

        if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Keep)
        {
            operationToken.ThrowIfCancellationRequested();
            await ApplyCurrentPlaybackStatusAsync(context, controlResult.PlayStatus).ConfigureAwait(false);
            return;
        }

        if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default &&
            controlResult.PlayStatus == PlayControlResult.PlayControlStatus.Play &&
            ReferenceEquals(previousContext, context))
        {
            var player = _pump.Player;
            if (player != null)
            {
                operationToken.ThrowIfCancellationRequested();
                await player.RestartAsync().ConfigureAwait(false);
            }
            return;
        }

        if (await LoadCoreAsync(lockAlreadyHeld: false, operationToken).ConfigureAwait(false))
        {
            operationToken.ThrowIfCancellationRequested();
            await ApplyLoadedPlaybackStatusAsync(context, controlResult.PlayStatus, playInstantly)
                .ConfigureAwait(false);
        }
    }

    private async Task ApplyCurrentPlaybackStatusAsync(
        BeatmapContext context,
        PlayControlResult.PlayControlStatus playStatus)
    {
        var player = _pump.Player;
        if (player == null) return;

        switch (playStatus)
        {
            case PlayControlResult.PlayControlStatus.Play:
                await player.RestartAsync().ConfigureAwait(false);
                break;
            case PlayControlResult.PlayControlStatus.Stop:
                await player.StopAsync().ConfigureAwait(false);
                break;
        }
    }

    private async Task ApplyLoadedPlaybackStatusAsync(
        BeatmapContext context,
        PlayControlResult.PlayControlStatus playStatus,
        bool playInstantly)
    {
        var player = _pump.Player;
        if (player == null) return;

        switch (playStatus)
        {
            case PlayControlResult.PlayControlStatus.Play:
                if (playInstantly)
                {
                    await player.PlayAsync().ConfigureAwait(false);
                }
                break;
            case PlayControlResult.PlayControlStatus.Stop:
                await player.StopAsync().ConfigureAwait(false);
                break;
        }
    }

    private async Task ClearPlayerAsync()
    {
        var player = _pump.Player;
        if (player == null) return;

        _pump.DetachPlayer();

        await SafeStopExtensions.TryStopAsync(
            player.StopAsync, _logger, "Error while stopping player during clear.").ConfigureAwait(false);
        await SafeStopExtensions.TryDisposeAsync(
            async () => await player.DisposeAsync().ConfigureAwait(false),
            _logger, "Error while disposing player during clear.").ConfigureAwait(false);
    }

    private CancellationToken GetCurrentOperationToken()
    {
        lock (_operationGate)
        {
            return _cts.Token;
        }
    }

    private CancellationToken InterruptPrevOperation()
    {
        CancellationTokenSource previous;
        var next = new CancellationTokenSource();
        lock (_operationGate)
        {
            previous = _cts;
            _cts = next;
        }

        try
        {
            previous.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Race with Dispose — recreate below.
        }

        previous.Dispose();
        return next.Token;
    }

    private void OnPlayerPlayStatusChanged(PlayStatus status)
    {
        if (status != PlayStatus.Finished) return;
        _ = HandlePlaybackFinishedAsync();
    }

    private async Task HandlePlaybackFinishedAsync()
    {
        if (Interlocked.Exchange(ref _isHandlingPlaybackFinished, 1) == 1)
        {
            return;
        }

        try
        {
            await Task.Yield();
            await PlayByControlAsync(PlayControlType.Next, autoAdvance: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while handling playback finished.");
        }
        finally
        {
            Interlocked.Exchange(ref _isHandlingPlaybackFinished, 0);
        }
    }
}
