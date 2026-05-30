using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Coosu.Beatmap;
using Coosu.Beatmap.MetaData;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Annotations;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Media.Audio;

public sealed partial class ObservablePlayController : ObservableObject, IAsyncDisposable
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
    private SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private bool _isHandlingLoadFailure;

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservablePlayController()
        : this(new PlayerDataService())
    {
    }

    public ObservablePlayController(IPlayerDataStore playerData)
    {
        _playerData = playerData;
        PlayList = new PlayList(playerData);
        PlayList.AutoSwitched += PlayList_AutoSwitched;
        PlayList.SongListChanged += PlayList_SongListChanged;
#if DEBUG
        LoadError += ObservablePlayController_LoadError;
#endif
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
            Logger.Error(ex, "Load error while loading beatmap: {0}",
                Path.Combine(ctx.BeatmapDetail.BaseFolder ?? "", ctx.BeatmapDetail.MapPath ?? ""));
        }
        else
        {
            Logger.Error(ex, "Load error while loading beatmap.");
        }
    }

    public async Task PlayNewAsync([CanBeNull] Beatmap beatmap, bool playInstantly = true)
    {
        if (beatmap is null) return;
        await PlayList.AddOrSwitchToAsync(beatmap);
        InitializeContextHandle(PlayList.CurrentInfo);
        if (await LoadAsync(false, playInstantly).ConfigureAwait(false))
        {
            if (playInstantly) await PlayList.CurrentInfo.PlayHandle.Invoke();
        }
    }

    public async Task PlayNewAsync(string path, bool playInstantly = true)
    {
        try
        {
            await _readLock.WaitAsync(_cts.Token).ConfigureAwait(false);
            IsFileLoading = true;

            if (!File.Exists(path))
                throw new FileNotFoundException("cannot locate file", path);

            Logger.Info("Start load new song from path: {0}", path);
            if (PlayList.CurrentInfo == null)
            {
                PlayList.InitializeEmptyCurrentInfo();
            }

            var context = PlayList.CurrentInfo;
            context.BeatmapDetail.MapPath = path;
            context.BeatmapDetail.BaseFolder = Path.GetDirectoryName(path);

            await ClearPlayer().ConfigureAwait(false);
            Execute.OnUiThread(() => PreLoadStarted?.Invoke(path, _cts.Token));
            var osuFile = await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor"))
                .ConfigureAwait(false); //50 ms

            context.OsuFile = osuFile;

            var beatmap = BeatmapExtension.ParseFromOSharp(osuFile);
            var trueBeatmap = await _playerData.GetBeatmapByIdentifiableAsync(beatmap);

            if (trueBeatmap == null)
            {
                trueBeatmap = beatmap;
                trueBeatmap.FolderName = path; // I forgot why I did this but there should be some reasons.
            }

            await PlayList.AddOrSwitchToAsync(trueBeatmap);

            InitializeContextHandle(context);
            if (await LoadAsync(true, playInstantly).ConfigureAwait(false))
            {
                if (playInstantly) await context.PlayHandle.Invoke().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var currentInfo = PlayList.CurrentInfo;
            LoadError?.Invoke(currentInfo, ex);
            Logger.Error(ex, "Error while loading new beatmap. BeatmapId: {0}; BeatmapSetId: {1}",
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

            var beatmap = context.Beatmap;
            Execute.OnUiThread(() => LoadStarted?.Invoke(context, _cts.Token));

            // meta
            var osuFile = context.OsuFile;
            var beatmapDetail = context.BeatmapDetail;

            var folder = beatmap.GetFolder(out var isFromDb, out var freePath)?.Trim();
            if (osuFile == null)
            {
                Logger.Info("Start load new song from db: {0}", beatmap.BeatmapFileName);
                var path = ResolveBeatmapPath(folder, beatmap.BeatmapFileName, isFromDb, freePath);
                beatmapDetail.MapPath = path;
                beatmapDetail.BaseFolder = Path.GetDirectoryName(path);

                osuFile = await OsuFile.ReadFromFileAsync(path).ConfigureAwait(false);
                context.OsuFile = osuFile;
            }

            var album = await _playerData.GetCollectionsByMapAsync(context.BeatmapSettings);

            bool isFavorite = album != null && album.Count > 0 && album.Any(k => k.LockedBool);
            var metadata = beatmapDetail.Metadata;
            metadata.IsFavorite = isFavorite;

            metadata.Artist = osuFile.Metadata.ArtistMeta;
            metadata.Title = osuFile.Metadata.TitleMeta;
            metadata.BeatmapId = osuFile.Metadata.BeatmapId;
            metadata.BeatmapsetId = osuFile.Metadata.BeatmapSetId;
            metadata.Creator = osuFile.Metadata.Creator;
            metadata.Version = osuFile.Metadata.Version;
            metadata.Source = osuFile.Metadata.Source;
            metadata.Tags = osuFile.Metadata.TagList;

            metadata.HP = osuFile.Difficulty.HpDrainRate;
            metadata.CS = osuFile.Difficulty.CircleSize;
            metadata.AR = osuFile.Difficulty.ApproachRate;
            metadata.OD = osuFile.Difficulty.OverallDifficulty;

            Execute.OnUiThread(() => MetaLoaded?.Invoke(context, _cts.Token));

            // background
            var defaultPath = Path.Combine(Domain.ResourcePath, "official", "registration.jpg");

            if (osuFile.Events.BackgroundInfo != null)
            {
                var bgPath = TryResolveChildPath(beatmapDetail.BaseFolder, osuFile.Events.BackgroundInfo.Filename);
                beatmapDetail.BackgroundPath = File.Exists(bgPath)
                    ? bgPath
                    : File.Exists(defaultPath)
                        ? defaultPath
                        : null;
            }
            else
            {
                beatmapDetail.BackgroundPath = File.Exists(defaultPath)
                    ? defaultPath
                    : null;
            }

            Execute.OnUiThread(() => BackgroundInfoLoaded?.Invoke(context, _cts.Token));

            // music
            beatmapDetail.MusicPath = ResolveChildPath(beatmapDetail.BaseFolder, osuFile.General.AudioFilename);

            if (PlayList.PreInfo?.BeatmapDetail?.BaseFolder != PlayList.CurrentInfo?.BeatmapDetail?.BaseFolder)
            {
                CachedSoundFactory.ClearCacheSounds();
            }

            Player = new OsuMixPlayer(osuFile, beatmapDetail.BaseFolder);
            Player.PlayStatusChanged += Player_PlayStatusChanged;
            Player.PositionUpdated += Player_PositionUpdated;
            await Player.Initialize().ConfigureAwait(false); //700 ms
            Player.ManualOffset = context.BeatmapSettings.Offset;

            Execute.OnUiThread(() => MusicLoaded?.Invoke(context, _cts.Token));

            // video
            var videoName = osuFile.Events.VideoInfo?.Filename;

            if (videoName != null)
            {
                var videoPath = TryResolveChildPath(beatmapDetail.BaseFolder, videoName);
                if (File.Exists(videoPath))
                {
                    beatmapDetail.VideoPath = videoPath;
                    Execute.OnUiThread(() => VideoLoadRequested?.Invoke(context, _cts.Token));
                }
            }

            // storyboard
            if (!string.IsNullOrWhiteSpace(osuFile.Events.StoryboardText))
            {
                Execute.OnUiThread(() => StoryboardLoadRequested?.Invoke(context, _cts.Token));
            }
            else
            {
                if (StoryboardFileHelper.HasOsbStoryboard(osuFile, beatmapDetail.MapPath))
                {
                    Execute.OnUiThread(() => StoryboardLoadRequested?.Invoke(context, _cts.Token));
                }
            }

            context.FullLoaded = true;
            // load finished
            Execute.OnUiThread(() => LoadFinished?.Invoke(context, _cts.Token));
            AppSettings.Default.CurrentMap = beatmap.GetIdentity();
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
            Logger.Error(ex, "Error while loading new beatmap. BeatmapId: {0}; BeatmapSetId: {1}",
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
            Logger.Warn(ex, "Player was already disposed while stopping.");
        }

        try
        {
            await player.DisposeAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException ex)
        {
            Logger.Warn(ex, "Player was already disposed while disposing.");
        }
    }

    private async void Player_PlayStatusChanged(PlayStatus obj)
    {
        // MixPlayer raises this through its audio STA. Posting to UI avoids UI<->audio Send deadlocks.
        Execute.ToUiThread(() =>
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
        Execute.ToUiThread(() => PositionUpdated?.Invoke(position));
    }

    private async Task PlayList_AutoSwitched(PlayControlResult controlResult, Beatmap beatmap, bool playInstantly)
    {
        try
        {
            var context = PlayList.CurrentInfo;

            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Keep)
            {
                await context.SetTimeHandle(0, playInstantly ||
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
                            if (playInstantly) await context.PlayHandle().ConfigureAwait(false);
                            break;
                        case PlayControlResult.PlayControlStatus.Stop:
                            await context.StopHandle().ConfigureAwait(false);
                            break;
                    }
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                Execute.OnUiThread(() => InterfaceClearRequest?.Invoke());
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error while auto changing song.");
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
                    Execute.OnUiThread(() => InterfaceClearRequest?.Invoke());
                    return;
                }

                if (preInfo == PlayList.CurrentInfo)
                {
                    await PlayList.CurrentInfo.StopHandle().ConfigureAwait(false);
                    await PlayList.CurrentInfo.PlayHandle().ConfigureAwait(false);
                    return;
                }

                InitializeContextHandle(PlayList.CurrentInfo);
                if (await LoadAsync(false, true).ConfigureAwait(false))
                {
                    await PlayList.CurrentInfo.PlayHandle.Invoke().ConfigureAwait(false);
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Keep)
            {
                switch (controlResult.PlayStatus)
                {
                    case PlayControlResult.PlayControlStatus.Play:
                        await PlayList.CurrentInfo.RestartHandle.Invoke().ConfigureAwait(false);
                        break;
                    case PlayControlResult.PlayControlStatus.Stop:
                        await PlayList.CurrentInfo.StopHandle.Invoke().ConfigureAwait(false);
                        break;
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                await ClearPlayer().ConfigureAwait(false);
                Execute.OnUiThread(() => InterfaceClearRequest?.Invoke());
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error while changing song.");
        }
    }

    private void InitializeContextHandle(BeatmapContext context)
    {
        context.PlayHandle = PlayAsync;
        context.PauseHandle = PauseAsync;
        context.StopHandle = StopAsync;
        context.RestartHandle = RestartAsync;
        context.TogglePlayHandle = TogglePlayAsync;
        context.SetTimeHandle = SetTimeAsync;
    }

    private bool TryGetReadyPlayer(out BeatmapContext context, out OsuMixPlayer player)
    {
        context = PlayList.CurrentInfo;
        player = Player;
        return context != null && player != null && player.PlayStatus != PlayStatus.Unknown;
    }

    private static string ResolveBeatmapPath(string folder, string beatmapFileName, bool isFromDb, string freePath)
    {
        if (!isFromDb)
        {
            if (string.IsNullOrWhiteSpace(freePath))
            {
                throw new InvalidDataException("Beatmap path is empty.");
            }

            return freePath;
        }

        return ResolveChildPath(folder, beatmapFileName);
    }

    private static string ResolveChildPath(string baseFolder, string childPath)
    {
        if (string.IsNullOrWhiteSpace(baseFolder))
        {
            throw new InvalidDataException("Beatmap base folder is empty.");
        }

        if (string.IsNullOrWhiteSpace(childPath))
        {
            throw new InvalidDataException("Beatmap referenced file path is empty.");
        }

        return Path.Combine(baseFolder, childPath);
    }

    private static string TryResolveChildPath(string baseFolder, string childPath)
    {
        return string.IsNullOrWhiteSpace(baseFolder) || string.IsNullOrWhiteSpace(childPath)
            ? null
            : Path.Combine(baseFolder, childPath);
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
                Logger.Error(ex, "Error while setting synchronized playback position.");
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
        if (Player != null) await Player.DisposeAsync().ConfigureAwait(false);
        _readLock?.Dispose();
        Logger.Debug($"Disposed {nameof(_readLock)}");
        _cts?.Dispose();
        Logger.Debug($"Disposed {nameof(_cts)}");
    }
}