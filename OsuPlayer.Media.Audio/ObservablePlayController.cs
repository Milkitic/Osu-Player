using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Media.Audio.Wave;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared;
using OSharp.Beatmap;
using OSharp.Beatmap.MetaData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Presentation.Annotations;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Media.Audio
{

    public sealed class ObservablePlayController : VmBase, IAsyncDisposable
    {
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan> PositionUpdated;

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

        public bool IsFileLoading
        {
            get => _isFileLoading;
            private set
            {
                if (value == _isFileLoading) return;
                _isFileLoading = value;
                OnPropertyChanged();
            }
        }

        public OsuMixPlayer Player
        {
            get => _player;
            private set
            {
                if (Equals(value, _player)) return;
                _player = value;
                OnPropertyChanged();
            }
        }

        public PlayList PlayList { get; } = new PlayList();
        public bool IsPlayerReady => Player != null && Player.PlayStatus != PlayStatus.Unknown;

        private OsuMixPlayer _player;
        private SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isFileLoading;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ObservablePlayController()
        {
            PlayList.AutoSwitched += PlayList_AutoSwitched;
            PlayList.SongListChanged += PlayList_SongListChanged;
#if DEBUG
            LoadError += ObservablePlayController_LoadError;
#endif
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
            PlayList.AddOrSwitchTo(beatmap);
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
                var context = PlayList.CurrentInfo;
                context.BeatmapDetail.MapPath = path;
                context.BeatmapDetail.BaseFolder = Path.GetDirectoryName(path);

                await ClearPlayer().ConfigureAwait(false);
                Execute.OnUiThread(() => PreLoadStarted?.Invoke(path, _cts.Token));
                var osuFile =
                    await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor"))
                        .ConfigureAwait(false); //50 ms
                if (!osuFile.ReadSuccess) throw osuFile.ReadException;

                context.OsuFile = osuFile;

                var beatmap = BeatmapConvertExtension.ParseFromOSharp(osuFile);

                Beatmap trueBeatmap;
                await using (var dbContext = new ApplicationDbContext())
                    trueBeatmap = await dbContext.Beatmaps.FindAsync(beatmap.Id);

                if (trueBeatmap == null)
                {
                    trueBeatmap = beatmap;
                    trueBeatmap.FolderNameOrPath = path; // I forgot why I did this but there should be some reasons.
                }

                PlayList.AddOrSwitchTo(trueBeatmap);

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

                var folder = beatmap.GetFolder(out var isFromDb, out var freePath).Trim();
                if (osuFile == null)
                {
                    Logger.Info("Start load new song from db: {0}", beatmap.BeatmapFileName);
                    string path = isFromDb ? Path.Combine(folder, beatmap.BeatmapFileName) : freePath;
                    beatmapDetail.MapPath = path;
                    beatmapDetail.BaseFolder = Path.GetDirectoryName(path);

                    osuFile = await OsuFile.ReadFromFileAsync(path).ConfigureAwait(false);
                    if (!osuFile.ReadSuccess) throw osuFile.ReadException;
                    context.OsuFile = osuFile;
                }

                await using var dbContext = new ApplicationDbContext();
                var album = await dbContext.GetCollectionsByBeatmap(context.BeatmapConfig);

                bool isFavorite = album.Any(k => k.IsLocked);
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
                    var bgPath = Path.Combine(beatmapDetail.BaseFolder,
                        osuFile.Events.BackgroundInfo.Filename);
                    beatmapDetail.BackgroundPath = File.Exists(bgPath)
                        ? bgPath
                        : File.Exists(defaultPath) ? defaultPath : null;
                }
                else
                {
                    beatmapDetail.BackgroundPath = File.Exists(defaultPath)
                        ? defaultPath
                        : null;
                }

                Execute.OnUiThread(() => BackgroundInfoLoaded?.Invoke(context, _cts.Token));

                // music
                beatmapDetail.MusicPath = Path.Combine(beatmapDetail.BaseFolder,
                    osuFile.General.AudioFilename);

                if (PlayList.PreInfo?.BeatmapDetail?.BaseFolder != PlayList.CurrentInfo?.BeatmapDetail?.BaseFolder)
                {
                    CachedSound.ClearCacheSounds();
                }

                Player = new OsuMixPlayer(osuFile, beatmapDetail.BaseFolder);
                Player.PlayStatusChanged += Player_PlayStatusChanged;
                Player.PositionUpdated += Player_PositionUpdated;
                await Player.Initialize().ConfigureAwait(false); //700 ms
                Player.ManualOffset = context.BeatmapConfig.Offset;

                Execute.OnUiThread(() => MusicLoaded?.Invoke(context, _cts.Token));

                // video
                var videoName = osuFile.Events.VideoInfo?.Filename;

                if (videoName != null)
                {
                    var videoPath = Path.Combine(beatmapDetail.BaseFolder, videoName);
                    if (File.Exists(videoPath))
                    {
                        beatmapDetail.VideoPath = videoPath;
                        Execute.OnUiThread(() => VideoLoadRequested?.Invoke(context, _cts.Token));
                    }
                }

                // storyboard
                var analyzer = new OsuFileAnalyzer(osuFile);
                if (osuFile.Events.ElementGroup.ElementList.Count > 0)
                    Execute.OnUiThread(() => StoryboardLoadRequested?.Invoke(context, _cts.Token));
                else
                {
                    var osbFile = Path.Combine(beatmapDetail.BaseFolder, analyzer.OsbFileName);
                    if (File.Exists(osbFile) && await OsuFile.OsbFileHasStoryboard(osbFile).ConfigureAwait(false))
                        Execute.OnUiThread(() => StoryboardLoadRequested?.Invoke(context, _cts.Token));
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

                if (Player?.PlayStatus != PlayStatus.Playing)
                {
                    await PlayByControl(PlayControlType.Next, false).ConfigureAwait(false);
                }

                return false;
            }
            finally
            {
                await using var dbContext = new ApplicationDbContext();
                await dbContext.AddOrUpdateBeatmapToRecent(context.Beatmap);
            }
        }

        private async Task ClearPlayer()
        {
            if (Player == null) return;
            await PlayList.CurrentInfo.StopHandle().ConfigureAwait(false);
            Player.PlayStatusChanged -= Player_PlayStatusChanged;
            Player.PositionUpdated -= Player_PositionUpdated;
            await Player.DisposeAsync().ConfigureAwait(false);
        }

        private async void Player_PlayStatusChanged(PlayStatus obj)
        {
            Execute.OnUiThread(() => PlayStatusChanged?.Invoke(obj));
            SharedVm.Default.IsPlaying = obj == PlayStatus.Playing;
            if (obj == PlayStatus.Finished)
                await PlayByControl(PlayControlType.Next, true).ConfigureAwait(false);
        }

        private void Player_PositionUpdated(TimeSpan position)
        {
            Execute.OnUiThread(() => PositionUpdated?.Invoke(position));
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
            context.PlayHandle = async () => await Player.Play().ConfigureAwait(false);
            context.PauseHandle = async () => await Player.Pause().ConfigureAwait(false);
            context.StopHandle = async () => await Player.Stop().ConfigureAwait(false);
            context.RestartHandle = async () =>
            {
                await context.StopHandle().ConfigureAwait(false);
                await context.PlayHandle().ConfigureAwait(false);
            };
            context.TogglePlayHandle = async () =>
            {
                if (Player.PlayStatus == PlayStatus.Ready ||
                    Player.PlayStatus == PlayStatus.Finished ||
                    Player.PlayStatus == PlayStatus.Paused)
                {
                    await context.PlayHandle().ConfigureAwait(false);
                }
                else if (Player.PlayStatus == PlayStatus.Playing) await context.PauseHandle().ConfigureAwait(false);
            };

            context.SetTimeHandle = async (time, play) =>
                await Player.SkipTo(TimeSpan.FromMilliseconds(time)).ConfigureAwait(false);
        }

        private void InterruptPrevOperation()
        {
            _cts.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        public async ValueTask DisposeAsync()
        {
            if (_player != null) await _player.DisposeAsync().ConfigureAwait(false);
            _readLock?.Dispose();
            Logger.Debug($"Disposed {nameof(_readLock)}");
            _cts?.Dispose();
            Logger.Debug($"Disposed {nameof(_cts)}");
        }
    }
}