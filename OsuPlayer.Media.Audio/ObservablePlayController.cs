using System;
using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Media.Audio.Core;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Media.Audio
{
    public class BeatmapLoadContext
    {
        public Beatmap Beatmap { get; set; }
        public BeatmapDetail BeatmapDetail { get; set; }
        public bool IsDetailInit => BeatmapDetail != null;
    }

    public class ObservablePlayController : ViewModelBase
    {
        private ComponentPlayer _player;
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan, TimeSpan> ProgressUpdated;

        public event Action InterfaceClearRequest;

        public event Action<Beatmap, CancellationToken> LoadStarted;

        public event Action<Beatmap, CancellationToken> MetaLoaded;
        public event Action<Beatmap, CancellationToken> BackgroundInfoLoaded;
        public event Action<Beatmap, CancellationToken> MusicLoaded;
        public event Action<Beatmap, CancellationToken> VideoLoadRequested;
        public event Action<Beatmap, CancellationToken> StoryboardLoadRequested;

        public event Action<Beatmap, CancellationToken> LoadFinished;

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

        public PlayList PlayList { get; } = new PlayList();

        public ObservablePlayController()
        {
            PlayList.AutoSwitched += PlayList_AutoSwitched;
        }

        public async Task PlayNewAsync(Beatmap Beatmap)
        {
            PlayList.AddOrSwitchTo(Beatmap);
            await LoadAsync(Beatmap);
            Player.Play();
        }

        public async Task PrevAsync()
        {
            await PlayByControl(PlayControlType.Previous, false);
        }

        public async Task NextAsync()
        {
            await PlayByControl(PlayControlType.Next, false);
        }

        public async Task LoadAsync(Beatmap Beatmap)
        {
            try
            {
                await _readLock.WaitAsync(_cts.Token);

                LoadStarted?.Invoke(Beatmap, _cts.Token);
                ClearPlayer();

                await Task.Delay(TimeSpan.FromSeconds(2), _cts.Token);
                MetaLoaded?.Invoke(Beatmap, _cts.Token);
                BackgroundInfoLoaded?.Invoke(Beatmap, _cts.Token);

                Player = new ComponentPlayer();
                Player.PlayStatusChanged += Player_PlayStatusChanged;
                Player.PositionChanged += Player_ProgressUpdated;
                MusicLoaded?.Invoke(Beatmap, _cts.Token);

                VideoLoadRequested?.Invoke(Beatmap, _cts.Token);
                StoryboardLoadRequested?.Invoke(Beatmap, _cts.Token);
                LoadFinished?.Invoke(Beatmap, _cts.Token);
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

                await LoadAsync(PlayList.CurrentInfo);
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
    }
}