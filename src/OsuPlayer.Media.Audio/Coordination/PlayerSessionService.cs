using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Media.Audio.Infrastructure;
using OsuPlayer.Media.Audio.Playlist;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Media.Audio.Coordination;

/// <summary>
/// Coordinates beatmap selection, loading, and playback commands for the
/// controller facade.
/// </summary>
/// <remarks>
/// This service owns the session operation token: user-initiated loads and
/// manual previous/next commands cancel superseded work, while auto-advance
/// continues the current operation. It serializes load/clear work with the
/// supplied <see cref="SemaphoreSlim"/>, attaches and detaches the active
/// <see cref="OsuMixPlayer"/> through <see cref="PlayerEventBus"/>, and translates
/// playlist cursor changes into player commands.
/// </remarks>
public sealed class PlayerSessionService : IAsyncDisposable
{
    private readonly PlayerEventBus _bus;
    private readonly PlayList _playList;
    private readonly BeatmapLoader _beatmapLoader;
    private readonly SemaphoreSlim _readLock;
    private readonly IPlayerDataStore _playerData;
    private readonly IPlaybackEngine _playbackEngine;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly ILogger<PlayerSessionService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SessionOperationManager _operations = new();
    private readonly Lock _disposeGate = new();

    private readonly TaskCompletionSource<object?> _disposeFinished =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isHandlingLoadFailure;
    private int _isHandlingPlaybackFinished;
    private bool _disposeStarted;

    public PlayerSessionService(
        PlayerEventBus bus,
        PlayList playList,
        BeatmapLoader beatmapLoader,
        SemaphoreSlim readLock,
        IPlayerDataStore playerData,
        IPlaybackEngine playbackEngine,
        AudioCacheManager audioCacheManager,
        ILogger<PlayerSessionService> logger,
        ILoggerFactory loggerFactory)
    {
        _bus = bus;
        _playList = playList;
        _beatmapLoader = beatmapLoader;
        _readLock = readLock;
        _playerData = playerData;
        _playbackEngine = playbackEngine;
        _audioCacheManager = audioCacheManager;
        _logger = logger;
        _loggerFactory = loggerFactory;

        _bus.PlayStatusChanged += OnPlayerPlayStatusChanged;
    }

    public async Task PlayNewFromBeatmapAsync(Beatmap beatmap, bool playInstantly)
    {
        using var operation = _operations.BeginInterruptingOperation();
        if (operation == null) return;

        await LoadAndPlayAsync(async token =>
        {
            token.ThrowIfCancellationRequested();
            await _playList.AddOrSwitchToAsync(beatmap).ConfigureAwait(false);
        }, playInstantly, operation.Token).ConfigureAwait(false);
    }

    public async Task PlayNewFromPathAsync(string path, bool playInstantly)
    {
        using var operation = _operations.BeginInterruptingOperation();
        if (operation == null) return;

        var operationToken = operation.Token;
        try
        {
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

            _bus.RaisePreLoadStarted(path, operationToken);

            var loaded = await LoadCoreAsync(
                operationToken,
                async (context, token) => await LoadOpenedPathAsync(context, path, token).ConfigureAwait(false),
                raiseLoadStartedBeforeLoad: false).ConfigureAwait(false);

            if (!operationToken.IsCancellationRequested && playInstantly && loaded && _bus.Player != null)
            {
                await _bus.Player.PlayAsync().ConfigureAwait(false);
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
    }

    public Task PlayPreviousAsync()
        => RunOperationAsync(
            _operations.BeginInterruptingOperation(),
            async operationToken =>
            {
                await _playList.MovePreviousAsync(wrap: true).ConfigureAwait(false);
                await LoadCurrentAsync(playInstantly: true, operationToken).ConfigureAwait(false);
            },
            "Error while changing song.");

    public Task PlayNextAsync(bool autoAdvance = false)
        => RunOperationAsync(
            autoAdvance ? _operations.BeginCurrentOperation() : _operations.BeginInterruptingOperation(),
            autoAdvance ? AutoAdvanceAsync : ManualNextAsync,
            "Error while changing song.");

    public Task ReplacePlaylistAsync(
        IEnumerable<Beatmap> beatmaps,
        bool startAnew,
        bool playInstantly,
        bool autoLoad)
        => RunOperationAsync(
            _operations.BeginInterruptingOperation(),
            async operationToken =>
            {
                await _playList.ReplaceAsync(beatmaps, startAnew).ConfigureAwait(false);
                if (autoLoad)
                {
                    await LoadCurrentAsync(playInstantly, operationToken).ConfigureAwait(false);
                }
            },
            "Error while replacing playlist.");

    public Task RemoveFromPlaylistAsync(IEnumerable<Beatmap> beatmaps)
        => RunOperationAsync(
            _operations.BeginInterruptingOperation(),
            async operationToken =>
            {
                var change = await _playList.RemoveAsync(beatmaps).ConfigureAwait(false);
                if (change.Changed)
                {
                    await LoadCurrentAsync(playInstantly: true, operationToken).ConfigureAwait(false);
                }
            },
            "Error while removing playlist entries.");

    public async ValueTask DisposeAsync()
    {
        bool ownsDispose;
        lock (_disposeGate)
        {
            ownsDispose = !_disposeStarted;
            _disposeStarted = true;
        }

        if (!ownsDispose)
        {
            await _disposeFinished.Task.ConfigureAwait(false);
            return;
        }

        _bus.PlayStatusChanged -= OnPlayerPlayStatusChanged;

        try
        {
            await _operations.CancelAndDrainAsync().ConfigureAwait(false);
            await ClearPlayerAsync().ConfigureAwait(false);
            _readLock.Dispose();
        }
        finally
        {
            await _operations.DisposeAsync().ConfigureAwait(false);
            _disposeFinished.TrySetResult(null);
        }
    }

    private async Task LoadAndPlayAsync(
        Func<CancellationToken, Task> setup,
        bool playInstantly,
        CancellationToken operationToken)
    {
        try
        {
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

        var loaded = await LoadCoreAsync(operationToken).ConfigureAwait(false);
        if (playInstantly && !operationToken.IsCancellationRequested && loaded && _bus.Player != null)
        {
            await _bus.Player.PlayAsync().ConfigureAwait(false);
        }
    }

    private async Task RunOperationAsync(
        SessionOperationManager.Operation? operation,
        Func<CancellationToken, Task> action,
        string errorMessage)
    {
        using (operation)
        {
            if (operation == null) return;

            var operationToken = operation.Token;
            try
            {
                await action(operationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (operationToken.IsCancellationRequested)
            {
                // Superseded by a newer playback operation.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ErrorMessage}", errorMessage);
            }
        }
    }

    private async Task<bool> LoadCoreAsync(
        CancellationToken operationToken,
        Func<BeatmapContext, CancellationToken, Task<LoadedBeatmap>>? loadOverride = null,
        bool raiseLoadStartedBeforeLoad = true)
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
            scope = await LoadScope.AcquireAsync(_readLock, operationToken).ConfigureAwait(false);
            await ClearPlayerAsync().ConfigureAwait(false);

            operationToken.ThrowIfCancellationRequested();
            if (raiseLoadStartedBeforeLoad)
            {
                _bus.RaiseLoadStarted(context, operationToken);
            }

            var loadedBeatmap = loadOverride == null
                ? new LoadedBeatmap(context, await LoadBeatmapAsync(context, operationToken).ConfigureAwait(false))
                : await loadOverride(context, operationToken).ConfigureAwait(false);
            context = loadedBeatmap.Context;

            var loadResult = loadedBeatmap.LoadResult;
            context.ApplyLoadResult(loadResult, operationToken);
            if (!raiseLoadStartedBeforeLoad)
            {
                _bus.RaiseLoadStarted(context, operationToken);
            }

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
            shouldTryFallback = !_isHandlingLoadFailure
                                && _bus.Player?.PlayStatus != PlayStatus.Playing;
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
                await PlayNextAsync().ConfigureAwait(false);
            }
            finally
            {
                _isHandlingLoadFailure = false;
            }
        }

        return loaded;
    }

    private async Task<LoadedBeatmap> LoadOpenedPathAsync(
        BeatmapContext context,
        string path,
        CancellationToken operationToken)
    {
        var osuFile = await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor"))
            .ConfigureAwait(false);
        operationToken.ThrowIfCancellationRequested();
        context.OsuFile = osuFile;

        var loadResult = await _beatmapLoader.LoadFromOsuFileAsync(
            osuFile,
            path,
            context.BeatmapSettings,
            operationToken).ConfigureAwait(false);
        operationToken.ThrowIfCancellationRequested();

        await _playList.AddOrSwitchToAsync(loadResult.Beatmap).ConfigureAwait(false);

        var loadedContext = _playList.CurrentInfo ??
                            throw new InvalidOperationException("Playlist did not create a current beatmap context.");
        return new LoadedBeatmap(loadedContext, loadResult);
    }

    private async Task<BeatmapLoadResult> LoadBeatmapAsync(
        BeatmapContext context,
        CancellationToken operationToken)
    {
        if (context.OsuFile == null)
        {
            return await _beatmapLoader.LoadFromBeatmapAsync(
                context.Beatmap,
                context.BeatmapSettings,
                operationToken).ConfigureAwait(false);
        }

        return await _beatmapLoader.LoadFromOsuFileAsync(
            context.OsuFile,
            context.BeatmapDetail.MapPath,
            context.BeatmapSettings,
            operationToken).ConfigureAwait(false);
    }

    private async Task FinishLoadAsync(
        BeatmapContext context,
        BeatmapLoadResult loadResult,
        CancellationToken operationToken)
    {
        operationToken.ThrowIfCancellationRequested();
        _bus.RaiseMetaLoaded(context, operationToken);
        _bus.RaiseBackgroundInfoLoaded(context, operationToken);

        var player = new OsuMixPlayer(loadResult.OsuFile, loadResult.BaseFolder, _playbackEngine, _audioCacheManager, _loggerFactory.CreateLogger<OsuMixPlayer>());
        var attached = false;
        try
        {
            await player.Initialize().ConfigureAwait(false);
            operationToken.ThrowIfCancellationRequested();
            player.ManualOffset = context.BeatmapSettings?.Offset ?? 0;
            _bus.AttachPlayer(player);
            attached = true;
            operationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            if (attached && ReferenceEquals(_bus.Player, player))
            {
                _bus.DetachPlayer();
            }

            await SafeStopExtensions.TryAsync(
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
        _logger.LogError(ex, "Error while loading new beatmap. BeatmapId: {BeatmapId}; BeatmapSetId: {BeatmapSetId}",
            context?.Beatmap?.BeatmapId, context?.Beatmap?.BeatmapSetId);
    }

    private async Task ManualNextAsync(CancellationToken operationToken)
    {
        await _playList.MoveNextAsync(wrap: true).ConfigureAwait(false);
        await LoadCurrentAsync(playInstantly: true, operationToken).ConfigureAwait(false);
    }

    private async Task AutoAdvanceAsync(CancellationToken operationToken)
    {
        if (_playList.Mode == PlaylistMode.Single)
        {
            await StopCurrentAsync().ConfigureAwait(false);
            return;
        }

        if (_playList.Mode == PlaylistMode.SingleLoop)
        {
            await RestartCurrentAsync(operationToken).ConfigureAwait(false);
            return;
        }

        if (!_playList.HasItems)
        {
            await ClearPlaybackAndInterfaceAsync().ConfigureAwait(false);
            return;
        }

        if (!_playList.IsLoop && _playList.IsLast)
        {
            await _playList.SelectFirstAsync().ConfigureAwait(false);
            await LoadCurrentAsync(playInstantly: false, operationToken).ConfigureAwait(false);
            return;
        }

        await _playList.MoveNextAsync(wrap: _playList.IsLoop).ConfigureAwait(false);
        await LoadCurrentAsync(playInstantly: true, operationToken).ConfigureAwait(false);
    }

    private async Task LoadCurrentAsync(bool playInstantly, CancellationToken operationToken)
    {
        operationToken.ThrowIfCancellationRequested();
        if (_playList.CurrentInfo == null)
        {
            await ClearPlaybackAndInterfaceAsync().ConfigureAwait(false);
            return;
        }

        var loaded = await LoadCoreAsync(operationToken).ConfigureAwait(false);
        if (playInstantly && !operationToken.IsCancellationRequested && loaded && _bus.Player != null)
        {
            await _bus.Player.PlayAsync().ConfigureAwait(false);
        }
    }

    private async Task RestartCurrentAsync(CancellationToken operationToken)
    {
        var player = _bus.Player;
        if (player == null) return;

        operationToken.ThrowIfCancellationRequested();
        await player.RestartAsync().ConfigureAwait(false);
    }

    private async Task StopCurrentAsync()
    {
        var player = _bus.Player;
        if (player != null)
        {
            await player.StopAsync().ConfigureAwait(false);
        }
    }

    private async Task ClearPlaybackAndInterfaceAsync()
    {
        await ClearPlayerAsync().ConfigureAwait(false);
        _bus.RaiseInterfaceClearRequest();
    }

    private async Task ClearPlayerAsync()
    {
        var player = _bus.Player;
        if (player == null) return;

        _bus.DetachPlayer();

        await SafeStopExtensions.TryAsync(
            player.StopAsync, _logger, "Error while stopping player during clear.").ConfigureAwait(false);
        await SafeStopExtensions.TryAsync(
            async () => await player.DisposeAsync().ConfigureAwait(false),
            _logger, "Error while disposing player during clear.").ConfigureAwait(false);
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
            await PlayNextAsync(autoAdvance: true).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling playback finished.");
        }
        finally
        {
            Interlocked.Exchange(ref _isHandlingPlaybackFinished, 0);
        }
    }

    private readonly record struct LoadedBeatmap(BeatmapContext Context, BeatmapLoadResult LoadResult);
}
