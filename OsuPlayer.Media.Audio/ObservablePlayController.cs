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
        private Player _player;
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

        public Player Player
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

        public async Task PlayNewAsync(Beatmap beatmap, bool playInstantly)
        {
            PlayList.AddOrSwitchTo(beatmap);
            await LoadAsync(beatmap);
            if (playInstantly) Player.Play();
        }

        public async Task PlayNewAsync(string path, bool playInstantly)
        {
            try
            {
                await _readLock.WaitAsync(_cts.Token);

                if (!File.Exists(path))
                    throw new FileNotFoundException("cannot locate file", path);
                ClearPlayer();

                var osuFile =
                    await OsuFile.ReadFromFileAsync(path, options => options.ExcludeSection("Editor")); //50 ms
                var beatmap = Beatmap.ParseFromOSharp(osuFile);
                Beatmap trueBeatmap = _appDbOperator.GetBeatmapByIdentifiable(beatmap);

                var context = await BeatmapContext.CreateAsync(trueBeatmap ?? beatmap);
                context.OsuFile = osuFile;
                await LoadAsync(trueBeatmap, context);
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

        private async Task LoadAsync(Beatmap beatmap, BeatmapContext context = null)
        {
            try
            {
                if (context == null)
                {
                    await _readLock.WaitAsync(_cts.Token);
                    ClearPlayer();
                    context = await BeatmapContext.CreateAsync(beatmap);
                }

                beatmap = context.Beatmap;
                LoadStarted?.Invoke(context, _cts.Token);

                var osuFile = context.OsuFile;

                if (osuFile == null)
                {
                    string path = beatmap.InOwnDb
                        ? Path.Combine(Domain.CustomSongPath, beatmap.FolderName, beatmap.BeatmapFileName)
                        : Path.Combine(Domain.OsuSongPath, beatmap.FolderName, beatmap.BeatmapFileName);
                    context.OsuFile = await OsuFile.ReadFromFileAsync(path);
                }

                var album = _appDbOperator.GetCollectionsByMap(context.BeatmapSettings);
                bool isFavorite = album != null && album.Any(k => k.LockedBool);

                var metadata = context.BeatmapDetail.Metadata;
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

                var defaultPath = Path.Combine(Domain.ResourcePath, "default.jpg");

                if (osuFile.Events.BackgroundInfo != null)
                {
                    var bgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                    context.BeatmapDetail.BackgroundPath = File.Exists(bgPath)
                        ? bgPath
                        : File.Exists(defaultPath) ? defaultPath : null;
                }
                else
                {
                    context.BeatmapDetail.BackgroundPath = File.Exists(defaultPath)
                        ? defaultPath
                        : null;
                }

                BackgroundInfoLoaded?.Invoke(context, _cts.Token);

                // music
                Player = new ComponentPlayer();
                Player.PlayStatusChanged += Player_PlayStatusChanged;
                Player.PositionChanged += Player_ProgressUpdated;
                MusicLoaded?.Invoke(context, _cts.Token);

                VideoLoadRequested?.Invoke(context, _cts.Token);
                StoryboardLoadRequested?.Invoke(context, _cts.Token);
                LoadFinished?.Invoke(context, _cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (context == null) _readLock.Release();
            }
        }

        private void ClearPlayer()
        {
            if (Player == null) return;
            Player.Stop();
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

        private async void PlayList_AutoSwitched(PlayControlResult controlResult, Beatmap Beatmap)
        {
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default)
            {
                await LoadAsync(Beatmap);
                switch (controlResult.PlayStatus)
                {
                    case PlayControlResult.PlayControlStatus.Play:
                        Player.Play();
                        break;
                    case PlayControlResult.PlayControlStatus.Stop:
                        Player.Stop();
                        break;
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                InterfaceClearRequest?.Invoke();
            }
        }

        private async Task PlayByControl(PlayControlType control, bool auto)
        {
            if (!auto)
            {
                InterruptPrevOperation();
            }

            var preInfo = PlayList.CurrentInfo;
            var controlResult = auto ? PlayList.InvokeAutoNext() : PlayList.SwitchByControl(control);
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
                    Player.Stop();
                    Player.Play();
                    return;
                }

                await LoadAsync(PlayList.CurrentInfo.Beatmap);
                Player.Play();
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                ClearPlayer();
                InterfaceClearRequest?.Invoke();
                return;
            }
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