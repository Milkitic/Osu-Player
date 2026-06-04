using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Coosu.Beatmap;
using Coosu.Beatmap.MetaData;
using KeyAsio.Core.Audio;
using KeyAsio.Core.Audio.Caching;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Media.Audio;

public sealed partial class ObservablePlayController : ObservableObject, IPlaybackController, IAsyncDisposable
{
    public event Action<PlayStatus> PlayStatusChanged;
    public event Action<TimeSpan> PositionUpdated;
    public event Func<BeatmapContext, double, bool, Task> PositionSetRequested;

    public event Action InterfaceClearRequest;

    public event Action<string, CancellationToken> PreLoadStarted;

    public event Action<BeatmapContext, CancellationToken> LoadStarted;

    public event Action<BeatmapContext, CancellationToken> MetaLoaded;
    public event Action<BeatmapContext, CancellationToken> BackgroundInfoLoaded;
    public event Action<BeatmapContext, CancellationToken> MusicLoaded;
    public event Action<BeatmapContext, CancellationToken> VideoLoadRequested;
    public event Action<BeatmapContext, CancellationToken> StoryboardLoadRequested;

    public event Action<BeatmapContext, CancellationToken> LoadFinished;

    public event Action<BeatmapContext, Exception> LoadError;

    [ObservableProperty]
    public partial bool IsFileLoading { get; private set; }

    [ObservableProperty]
    public partial OsuMixPlayer Player { get; private set; }

    public PlayList PlayList { get; }
    public bool IsPlayerReady => Player != null && Player.PlayStatus != PlayStatus.Unknown;

    private readonly IPlayerDataStore _playerData;
    private readonly IPlaybackEngine _playbackEngine;
    private readonly IAudioDeviceManager _audioDeviceManager;
    private readonly AudioCacheManager _audioCacheManager;
    private readonly Action<Exception> _audioDeviceErrorHandler;
    private readonly IUiThreadDispatcher _uiThreadDispatcher;
    private readonly BeatmapLoader _beatmapLoader;
    private readonly BeatmapLoadService _loadService;
    private SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private bool _isHandlingLoadFailure;

    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservablePlayController(
        IPlayerDataStore playerData,
        IPlaybackEngine playbackEngine,
        IAudioDeviceManager audioDeviceManager,
        AudioCacheManager audioCacheManager,
        Action<Exception> audioDeviceErrorHandler,
        IUiThreadDispatcher uiThreadDispatcher)
    {
        _playerData = playerData;
        _playbackEngine = playbackEngine;
        _audioDeviceManager = audioDeviceManager;
        _audioCacheManager = audioCacheManager;
        _audioDeviceErrorHandler = audioDeviceErrorHandler;
        _uiThreadDispatcher = uiThreadDispatcher;
        _beatmapLoader = new BeatmapLoader(playerData);
        _loadService = new BeatmapLoadService(_beatmapLoader);
        _playbackEngine.DeviceError += PlaybackEngine_DeviceError;
        PlayList = new PlayList(playerData, uiThreadDispatcher);
        PlayList.AutoSwitched += PlayList_AutoSwitched;
        PlayList.SongListChanged += PlayList_SongListChanged;
#if DEBUG
        LoadError += ObservablePlayController_LoadError;
#endif
    }

    private void PlaybackEngine_DeviceError(Exception ex)
    {
        s_logger.Error(ex, "Audio device error.");
        _uiThreadDispatcher.Post(() => _audioDeviceErrorHandler?.Invoke(ex));
    }

    public async Task PlayAsync()
    {
        if (!TryGetReadyPlayer(out _, out var player)) return;
        await player.Play().ConfigureAwait(false);
    }

    public async Task PauseAsync()
    {
        if (!TryGetReadyPlayer(out _, out var player)) return;
        await player.Pause().ConfigureAwait(false);
    }

    public async Task StopAsync()
    {
        if (!TryGetReadyPlayer(out _, out var player)) return;
        await player.Stop().ConfigureAwait(false);
    }

    public async Task RestartAsync()
    {
        await StopAsync().ConfigureAwait(false);
        await PlayAsync().ConfigureAwait(false);
    }

    public async Task TogglePlayAsync()
    {
        if (!TryGetReadyPlayer(out _, out var player)) return;

        if (player.PlayStatus == PlayStatus.Ready ||
            player.PlayStatus == PlayStatus.Finished ||
            player.PlayStatus == PlayStatus.Paused)
        {
            await PlayAsync().ConfigureAwait(false);
        }
        else if (player.PlayStatus == PlayStatus.Playing)
        {
            await PauseAsync().ConfigureAwait(false);
        }
    }

    public async Task SetTimeAsync(double time, bool play)
    {
        if (!TryGetReadyPlayer(out var context, out var player)) return;
        await player.SkipTo(TimeSpan.FromMilliseconds(time)).ConfigureAwait(false);
        await RaisePositionSetRequestedAsync(context, time, play).ConfigureAwait(false);
    }

    private void ObservablePlayController_LoadError(BeatmapContext ctx, Exception ex)
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
    }

    public async Task PlayNewAsync(Beatmap? beatmap, bool playInstantly = true)
    {
        if (beatmap is null) return;
        await PlayList.AddOrSwitchToAsync(beatmap);
        InitializeContextHandle(PlayList.CurrentInfo);
        if (await LoadAsync(false, playInstantly).ConfigureAwait(false))
        {
            if (playInstantly) await PlayList.CurrentInfo.PlaybackController.PlayAsync();
        }
    }

    public async Task PlayNewAsync(string path, bool playInstantly = true)
    {
        try
        {
            await _readLock.WaitAsync(_cts.Token).ConfigureAwait(false);
            IsFileLoading = true;

            if (!File.Exists(path))
                throw new FileNotFoundException("Cannot locate file", path);

            s_logger.Info("Start load new song from path: {0}", path);
            if (PlayList.CurrentInfo == null)
            {
                PlayList.InitializeEmptyCurrentInfo();
            }

            var context = PlayList.CurrentInfo;
            context.BeatmapDetail.MapPath = path;
            context.BeatmapDetail.BaseFolder = Path.GetDirectoryName(path) ?? string.Empty;

            await ClearPlayer().ConfigureAwait(false);
            _uiThreadDispatcher.Send(() => PreLoadStarted?.Invoke(path, _cts.Token));

            // Pre-read the .osu file so LoadAsync(skipFileRead: true) can skip the I/O
            var osuFile = await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor"))
                .ConfigureAwait(false);
            context.OsuFile = osuFile;

            var loadResult = await _beatmapLoader.LoadFromOsuFileAsync(
                osuFile, path, context.BeatmapSettings, _cts.Token).ConfigureAwait(false);

            var trueBeatmap = loadResult.Beatmap;
            await PlayList.AddOrSwitchToAsync(trueBeatmap);

            InitializeContextHandle(context);
            if (await LoadAsync(true, playInstantly).ConfigureAwait(false))
            {
                if (playInstantly) await context.PlaybackController.PlayAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var currentInfo = PlayList.CurrentInfo;
            LoadError?.Invoke(currentInfo, ex);
            s_logger.Error(ex, "Error while loading new beatmap. BeatmapId: {0}; BeatmapSetId: {1}",
                currentInfo?.Beatmap?.BeatmapId, currentInfo?.Beatmap?.BeatmapSetId);
        }
        finally
        {
            IsFileLoading = false;
            _readLock.Release();
        }
    }

    public async Task PlayPrevAsync()
    {
        await PlayByControl(PlayControlType.Previous, false).ConfigureAwait(false);
    }

    public async Task PlayNextAsync()
    {
        await PlayByControl(PlayControlType.Next, false).ConfigureAwait(false);
    }

    private async Task<bool> LoadAsync(bool isReading, bool playInstantly)
    {
        var context = PlayList.CurrentInfo;
        context.PlayInstantly = playInstantly;
        try
        {
            if (!isReading)
            {
                await _readLock.WaitAsync(_cts.Token).ConfigureAwait(false);
                IsFileLoading = true;
                await ClearPlayer().ConfigureAwait(false);
            }

            _uiThreadDispatcher.Send(() => LoadStarted?.Invoke(context, _cts.Token));

            BeatmapLoadResult loadResult;
            if (context.OsuFile == null)
            {
                loadResult = await _loadService.LoadFromBeatmapAsync(context, _cts.Token).ConfigureAwait(false);
            }
            else
            {
                loadResult = await _loadService.LoadFromOsuFileAsync(
                    context, context.BeatmapDetail.MapPath, _cts.Token).ConfigureAwait(false);
            }

            _uiThreadDispatcher.Send(() => MetaLoaded?.Invoke(context, _cts.Token));
            _uiThreadDispatcher.Send(() => BackgroundInfoLoaded?.Invoke(context, _cts.Token));

            var player = new OsuMixPlayer(loadResult.OsuFile, loadResult.BaseFolder, _playbackEngine, _audioCacheManager);
            Player = player;
            player.PlayStatusChanged += Player_PlayStatusChanged;
            player.PositionUpdated += Player_PositionUpdated;
            await player.Initialize().ConfigureAwait(false);
            player.ManualOffset = context.BeatmapSettings.Offset;

            _uiThreadDispatcher.Send(() => MusicLoaded?.Invoke(context, _cts.Token));

            // video
            if (loadResult.VideoPath != null)
            {
                context.BeatmapDetail.VideoPath = loadResult.VideoPath;
                _uiThreadDispatcher.Send(() => VideoLoadRequested?.Invoke(context, _cts.Token));
            }

            // storyboard
            if (loadResult.HasStoryboard)
            {
                _uiThreadDispatcher.Send(() => StoryboardLoadRequested?.Invoke(context, _cts.Token));
            }

            context.FullLoaded = true;
            _uiThreadDispatcher.Send(() => LoadFinished?.Invoke(context, _cts.Token));
            AppSettings.Default.CurrentMap = context.Beatmap.GetIdentity();
            AppSettings.SaveDefault();
            if (!isReading)
            {
                IsFileLoading = false;
                _readLock.Release();
            }

            return true;
        }
        catch (Exception ex)
        {
            var currentInfo = PlayList.CurrentInfo;
            LoadError?.Invoke(currentInfo, ex);
            s_logger.Error(ex, "Error while loading new beatmap. BeatmapId: {0}; BeatmapSetId: {1}",
                currentInfo?.Beatmap?.BeatmapId, currentInfo?.Beatmap?.BeatmapSetId);

            if (!isReading)
            {
                IsFileLoading = false;
                _readLock.Release();
            }

            if (!_isHandlingLoadFailure && Player?.PlayStatus != PlayStatus.Playing)
            {
                _isHandlingLoadFailure = true;
                try
                {
                    await PlayByControl(PlayControlType.Next, false).ConfigureAwait(false);
                }
                finally
                {
                    _isHandlingLoadFailure = false;
                }
            }

            return false;
        }
        finally
        {
            await _playerData.TryUpdateMapAsync(context.Beatmap.GetIdentity());
        }
    }

    private async Task ClearPlayer()
    {
        var player = Player;
        if (player == null) return;

        Player = null;
        player.PlayStatusChanged -= Player_PlayStatusChanged;
        player.PositionUpdated -= Player_PositionUpdated;

        try
        {
            await player.Stop().ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            s_logger.Warn(ex, "Player was already disposed while stopping.");
        }

        try
        {
            await player.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            s_logger.Warn(ex, "Player was already disposed while disposing.");
        }
    }

    private async void Player_PlayStatusChanged(PlayStatus obj)
    {
        // MixPlayer raises this through its audio STA. Posting to UI avoids UI<->audio Send deadlocks.
        _uiThreadDispatcher.Post(() =>
        {
            PlayStatusChanged?.Invoke(obj);
            SharedVm.Default.IsPlaying = obj == PlayStatus.Playing;
        });

        if (obj == PlayStatus.Finished)
            await PlayByControl(PlayControlType.Next, true).ConfigureAwait(false);
    }

    private void Player_PositionUpdated(TimeSpan position)
    {
        // MixPlayer raises this through its audio STA. Posting to UI avoids UI<->audio Send deadlocks.
        _uiThreadDispatcher.Post(() => PositionUpdated?.Invoke(position));
    }

    private async Task PlayList_AutoSwitched(PlayControlResult controlResult, Beatmap beatmap, bool playInstantly)
    {
        try
        {
            var context = PlayList.CurrentInfo;

            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Keep)
            {
                await context.PlaybackController.SetTimeAsync(0, playInstantly ||
                                                controlResult.PlayStatus == PlayControlResult.PlayControlStatus.Play)
                    .ConfigureAwait(false);
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default ||
                     controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Reset)
            {
                InitializeContextHandle(context);
                if (await LoadAsync(false, true).ConfigureAwait(false))
                {
                    switch (controlResult.PlayStatus)
                    {
                        case PlayControlResult.PlayControlStatus.Play:
                            if (playInstantly) await context.PlaybackController.PlayAsync();
                            break;
                        case PlayControlResult.PlayControlStatus.Stop:
                            await context.PlaybackController.StopAsync();
                            break;
                    }
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                _uiThreadDispatcher.Send(() => InterfaceClearRequest?.Invoke());
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            s_logger.Error(ex, "Error while auto changing song.");
        }
    }

    private void PlayList_SongListChanged()
    {
        AppSettings.Default.CurrentList = new HashSet<MapIdentity>(PlayList.SongList.Select(k => k.GetIdentity()));
        AppSettings.SaveDefault();
    }

    private async Task PlayByControl(PlayControlType control, bool auto)
    {
        try
        {
            if (!auto)
            {
                InterruptPrevOperation();
            }

            var preInfo = PlayList.CurrentInfo;
            var controlResult = auto
                ? await PlayList.InvokeAutoNext().ConfigureAwait(false)
                : await PlayList.SwitchByControl(control).ConfigureAwait(false);
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default &&
                controlResult.PlayStatus == PlayControlResult.PlayControlStatus.Play)
            {
                if (PlayList.CurrentInfo == null)
                {
                    await ClearPlayer().ConfigureAwait(false);
                    _uiThreadDispatcher.Send(() => InterfaceClearRequest?.Invoke());
                    return;
                }

                if (preInfo == PlayList.CurrentInfo)
                {
                    await PlayList.CurrentInfo.PlaybackController.StopAsync().ConfigureAwait(false);
                    await PlayList.CurrentInfo.PlaybackController.PlayAsync().ConfigureAwait(false);
                    return;
                }

                InitializeContextHandle(PlayList.CurrentInfo);
                if (await LoadAsync(false, true).ConfigureAwait(false))
                {
                    await PlayList.CurrentInfo.PlaybackController.PlayAsync().ConfigureAwait(false);
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Keep)
            {
                switch (controlResult.PlayStatus)
                {
                    case PlayControlResult.PlayControlStatus.Play:
                        await PlayList.CurrentInfo.PlaybackController.RestartAsync().ConfigureAwait(false);
                        break;
                    case PlayControlResult.PlayControlStatus.Stop:
                        await PlayList.CurrentInfo.PlaybackController.StopAsync().ConfigureAwait(false);
                        break;
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                await ClearPlayer().ConfigureAwait(false);
                _uiThreadDispatcher.Send(() => InterfaceClearRequest?.Invoke());
                return;
            }
        }
        catch (Exception ex)
        {
            s_logger.Error(ex, "Error while changing song.");
        }
    }

    private void InitializeContextHandle(BeatmapContext context)
    {
        context.PlaybackController = this;
    }

    private bool TryGetReadyPlayer(out BeatmapContext context, out OsuMixPlayer player)
    {
        context = PlayList.CurrentInfo;
        player = Player;
        return context != null && player != null && player.PlayStatus != PlayStatus.Unknown;
    }

    private async Task RaisePositionSetRequestedAsync(BeatmapContext context, double time, bool play)
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
                s_logger.Error(ex, "Error while setting synchronized playback position.");
            }
        }
    }

    private void InterruptPrevOperation()
    {
        _cts.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
    }

    public async ValueTask DisposeAsync()
    {
        _playbackEngine.DeviceError -= PlaybackEngine_DeviceError;
        if (Player != null) await Player.DisposeAsync().ConfigureAwait(false);
        _readLock?.Dispose();
        s_logger.Debug($"Disposed {nameof(_readLock)}");
        _cts?.Dispose();
        s_logger.Debug($"Disposed {nameof(_cts)}");
    }
}
