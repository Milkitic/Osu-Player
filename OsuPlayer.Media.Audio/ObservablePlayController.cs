using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Core;
using Milky.WpfApi;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Media.Audio
{

    public sealed class ObservablePlayController : ViewModelBase, IDisposable
    {
        private ComponentPlayer _player;
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan, TimeSpan> ProgressUpdated;

        public event Action InterfaceClearRequest;

        public event Action<string, CancellationToken> PreLoadStarted;

        public event Action<BeatmapContext, CancellationToken> LoadStarted;

        public event Action<BeatmapContext, CancellationToken> MetaLoaded;
        public event Action<BeatmapContext, CancellationToken> BackgroundInfoLoaded;
        public event Action<BeatmapContext, CancellationToken> MusicLoaded;
        public event Action<BeatmapContext, CancellationToken> VideoLoadRequested;
        public event Action<BeatmapContext, CancellationToken> StoryboardLoadRequested;

        public event Action<BeatmapContext, CancellationToken> LoadFinished;

        public ComponentPlayer Player
        {
            get => _player;
            private set
            {
                if (Equals(value, _player)) return;
                _player = value;
                OnPropertyChanged();
            }
        }

        private SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly AppDbOperator _appDbOperator = new AppDbOperator();

        public PlayList PlayList { get; } = new PlayList();
        public bool IsPlayerReady => Player != null && Player.PlayStatus != PlayStatus.NotInitialized;

        public ObservablePlayController()
        {
            PlayList.AutoSwitched += PlayList_AutoSwitched;
        }

        public async Task PlayNewAsync(Beatmap beatmap, bool playInstantly = true)
        {
            PlayList.AddOrSwitchTo(beatmap);
            InitializeContextHandle(PlayList.CurrentInfo);
            await LoadAsync(false);
            if (playInstantly) PlayList.CurrentInfo.PlayHandle.Invoke();
        }

        public async Task PlayNewAsync(string path, bool playInstantly = true)
        {
            try
            {
                await _readLock.WaitAsync(_cts.Token);

                if (!File.Exists(path))
                    throw new FileNotFoundException("cannot locate file", path);
                ClearPlayer();
                PreLoadStarted?.Invoke(path, _cts.Token);
                var osuFile =
                    await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor")); //50 ms
                var beatmap = Beatmap.ParseFromOSharp(osuFile);
                beatmap.IsTemporary = true;
                Beatmap trueBeatmap = _appDbOperator.GetBeatmapByIdentifiable(beatmap) ?? beatmap;

                PlayList.AddOrSwitchTo(trueBeatmap);
                var context = PlayList.CurrentInfo;
                context.OsuFile = osuFile;
                context.BeatmapDetail.MapPath = path;
                context.BeatmapDetail.BaseFolder = Path.GetDirectoryName(path);

                context.PlayInstantly = playInstantly;
                InitializeContextHandle(context);
                await LoadAsync(true);
                if (playInstantly) context.PlayHandle.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _readLock.Release();
            }
        }

        public async Task PlayPrevAsync()
        {
            await PlayByControl(PlayControlType.Previous, false);
        }

        public async Task PlayNextAsync()
        {
            await PlayByControl(PlayControlType.Next, false);
        }

        private async Task LoadAsync(bool isReading)
        {
            var context = PlayList.CurrentInfo;
            try
            {
                if (!isReading)
                {
                    await _readLock.WaitAsync(_cts.Token);
                    ClearPlayer();
                }

                var beatmap = context.Beatmap;
                LoadStarted?.Invoke(context, _cts.Token);

                // meta
                var osuFile = context.OsuFile;
                var beatmapDetail = context.BeatmapDetail;

                if (osuFile == null)
                {
                    string path = beatmap.InOwnDb
                        ? Path.Combine(Domain.CustomSongPath, beatmap.FolderName, beatmap.BeatmapFileName)
                        : Path.Combine(Domain.OsuSongPath, beatmap.FolderName, beatmap.BeatmapFileName);
                    osuFile = await OsuFile.ReadFromFileAsync(path);
                    context.OsuFile = osuFile;
                    beatmapDetail.MapPath = path;
                    beatmapDetail.BaseFolder = Path.GetDirectoryName(path);
                }

                var album = _appDbOperator.GetCollectionsByMap(context.BeatmapSettings);
                bool isFavorite = album != null && album.Any(k => k.LockedBool);

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

                MetaLoaded?.Invoke(context, _cts.Token);

                // background
                var defaultPath = Path.Combine(Domain.ResourcePath, "default.jpg");

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

                BackgroundInfoLoaded?.Invoke(context, _cts.Token);

                // music
                beatmapDetail.MusicPath = Path.Combine(beatmapDetail.BaseFolder,
                    osuFile.General.AudioFilename);

                if (PlayList.PreInfo?.BeatmapDetail?.BaseFolder != PlayList.CurrentInfo?.BeatmapDetail?.BaseFolder)
                {
                    AudioPlaybackEngine.ClearCacheSounds();
                }

                Player = new ComponentPlayer(beatmapDetail.MapPath, osuFile)
                {
                    HitsoundOffset = context.BeatmapSettings.Offset
                };
                Player.PlayStatusChanged += Player_PlayStatusChanged;
                Player.PositionChanged += Player_ProgressUpdated;
                await Player.InitializeAsync(); //700 ms

                MusicLoaded?.Invoke(context, _cts.Token);

                // video
                var videoName = osuFile.Events.VideoInfo?.Filename;
                var videoPath = Path.Combine(beatmapDetail.BaseFolder, videoName);
                if (videoName != null && File.Exists(videoPath))
                {
                    beatmapDetail.VideoPath = videoPath;
                    VideoLoadRequested?.Invoke(context, _cts.Token);
                }

                // storyboard
                var analyzer = new OsuFileAnalyzer(osuFile);
                if (osuFile.Events.ElementGroup.ElementList.Count > 0)
                    StoryboardLoadRequested?.Invoke(context, _cts.Token);
                else
                {
                    var osbFile = Path.Combine(beatmapDetail.BaseFolder, analyzer.OsbFileName);
                    if (File.Exists(osbFile) && await OsuFile.OsbFileHasStoryboard(osbFile))
                        StoryboardLoadRequested?.Invoke(context, _cts.Token);
                }

                context.FullLoaded = true;
                // load finished
                LoadFinished?.Invoke(context, _cts.Token);
                AppSettings.Default.CurrentMap = beatmap.GetIdentity();
                AppSettings.SaveDefault();
            }
            catch (Exception ex)
            {
                Notification.Push(@"发生未处理的错误：" + (ex.InnerException?.Message ?? ex?.Message));

                if (Player.PlayStatus != PlayStatus.Playing)
                {
                    await PlayByControl(PlayControlType.Next, true);
                }
            }
            finally
            {
                if (!isReading) _readLock.Release();
                _appDbOperator.UpdateMap(context.Beatmap.GetIdentity());
            }
        }

        private void ClearPlayer()
        {
            if (Player == null) return;
            PlayList.CurrentInfo.StopHandle();
            Player.PlayStatusChanged -= Player_PlayStatusChanged;
            Player.PositionChanged -= Player_ProgressUpdated;
            Player.Dispose();
        }

        private async void Player_PlayStatusChanged(PlayStatus obj)
        {
            PlayStatusChanged?.Invoke(obj);
            if (obj == PlayStatus.Finished) await PlayByControl(PlayControlType.Next, true);
        }

        private void Player_ProgressUpdated(object sender, ProgressEventArgs e)
        {
            TimeSpan playTime = e.Position;
            TimeSpan duration = e.Duration;
            ProgressUpdated?.Invoke(playTime, duration);
        }

        private async Task PlayList_AutoSwitched(PlayControlResult controlResult, Beatmap beatmap, bool playInstantly)
        {
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default)
            {
                var context = PlayList.CurrentInfo;
                InitializeContextHandle(context);
                await LoadAsync(false);
                switch (controlResult.PlayStatus)
                {
                    case PlayControlResult.PlayControlStatus.Play:
                        if (playInstantly) context.PlayHandle();
                        break;
                    case PlayControlResult.PlayControlStatus.Stop:
                        context.StopHandle();
                        break;
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                InterfaceClearRequest?.Invoke();
            }

            await Task.CompletedTask;
        }

        private async Task PlayByControl(PlayControlType control, bool auto)
        {
            if (!auto)
            {
                InterruptPrevOperation();
            }

            var preInfo = PlayList.CurrentInfo;
            var controlResult = auto ? await PlayList.InvokeAutoNext() : await PlayList.SwitchByControl(control);
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default &&
                controlResult.PlayStatus == PlayControlResult.PlayControlStatus.Play)
            {
                if (PlayList.CurrentInfo == null)
                {
                    ClearPlayer();
                    InterfaceClearRequest?.Invoke();
                    return;
                }

                if (preInfo == PlayList.CurrentInfo)
                {
                    PlayList.CurrentInfo.StopHandle();
                    PlayList.CurrentInfo.PlayHandle();
                    return;
                }

                InitializeContextHandle(PlayList.CurrentInfo);
                await LoadAsync(false);
                PlayList.CurrentInfo.PlayHandle.Invoke();
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                ClearPlayer();
                InterfaceClearRequest?.Invoke();
                return;
            }
        }

        private void InitializeContextHandle(BeatmapContext context)
        {
            context.PlayHandle = () => Player.Play();
            context.PauseHandle = () => Player.Pause();
            context.StopHandle = () => Player.Stop();
            context.SetTimeHandle = (time, play) => Player.SetTime(TimeSpan.FromMilliseconds(time), play);
        }

        private void InterruptPrevOperation()
        {
            _cts.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _player?.Dispose();
            _readLock?.Dispose();
            _cts?.Dispose();
        }
    }
}