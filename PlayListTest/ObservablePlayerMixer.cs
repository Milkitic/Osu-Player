using System;
using System.Threading.Tasks;
using PlayListTest.Models;

namespace PlayListTest
{
    public class ObservablePlayerMixer : VmBase
    {
        private Player _player;
        public event Action<PlayStatus> PlayStatusChanged;
        public event Action<TimeSpan, TimeSpan> ProgressUpdated;

        public event Action InterfaceClearRequest;

        public event Action<SongInfo> LoadStarted;

        public event Action<SongInfo> MetaLoaded;
        public event Action<SongInfo> BackgroundInfoLoaded;
        public event Action<SongInfo> MusicLoaded;
        public event Action<SongInfo> VideoLoadRequested;
        public event Action<SongInfo> StoryboardLoadRequested;

        public event Action<SongInfo> LoadFinished;

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

        public PlayList PlayList { get; } = new PlayList();

        public ObservablePlayerMixer()
        {
            PlayList.AutoSwitched += PlayList_AutoSwitched;
        }

        public async Task PlayNewAsync(SongInfo songInfo)
        {
            PlayList.AddOrSwitchTo(songInfo);
            await LoadAsync(songInfo);
            Player.Play();
        }

        public async Task PrevAsync()
        {
            await PlayByControl(PlayControl.Previous);
        }

        public async Task NextAsync()
        {
            await PlayByControl(PlayControl.Next);
        }

        public async Task LoadAsync(SongInfo songInfo)
        {
            LoadStarted?.Invoke(songInfo);
            if (Player != null)
            {
                Player.PlayStatusChanged -= Player_PlayStatusChanged;
                Player.ProgressUpdated -= Player_ProgressUpdated;
                Player.Dispose();
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
            MetaLoaded?.Invoke(songInfo);
            BackgroundInfoLoaded?.Invoke(songInfo);

            Player = new Player();
            Player.PlayStatusChanged += Player_PlayStatusChanged;
            Player.ProgressUpdated += Player_ProgressUpdated;
            MusicLoaded?.Invoke(songInfo);

            VideoLoadRequested?.Invoke(songInfo);
            StoryboardLoadRequested?.Invoke(songInfo);
            LoadFinished?.Invoke(songInfo);
        }

        private void Player_PlayStatusChanged(PlayStatus obj)
        {
            PlayStatusChanged?.Invoke(obj);
            if (obj == PlayStatus.Finished) PlayList.InvokeAutoNext();
        }

        private void Player_ProgressUpdated(TimeSpan playTime, TimeSpan duration)
        {
            ProgressUpdated?.Invoke(playTime, duration);
        }

        private async void PlayList_AutoSwitched(PlayControlResult controlResult, SongInfo songInfo)
        {
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default)
            {
                await LoadAsync(songInfo);
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

        private async Task PlayByControl(PlayControl control)
        {
            var controlResult = PlayList.SwitchByControl(control);
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default &&
                controlResult.PlayStatus == PlayControlResult.PlayControlStatus.Play)
            {
                await LoadAsync(PlayList.CurrentInfo);
                Player.Play();
            }
        }
    }
}