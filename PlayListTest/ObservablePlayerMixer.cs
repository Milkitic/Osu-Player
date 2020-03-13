﻿using System;
using System.Threading;
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

        private SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts = new CancellationTokenSource();

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
            await PlayByControl(PlayControl.Previous, false);
        }

        public async Task NextAsync()
        {
            await PlayByControl(PlayControl.Next, false);
        }

        public async Task LoadAsync(SongInfo songInfo)
        {
            await _readLock.WaitAsync();

            try
            {
                LoadStarted?.Invoke(songInfo);
                ClearPlayer();

                await Task.Delay(TimeSpan.FromSeconds(2), _cts.Token);
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
            Player.ProgressUpdated -= Player_ProgressUpdated;
            Player.Dispose();
        }

        private async void Player_PlayStatusChanged(PlayStatus obj)
        {
            PlayStatusChanged?.Invoke(obj);
            if (obj == PlayStatus.Finished) await PlayByControl(PlayControl.Next, true);
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

        private async Task PlayByControl(PlayControl control, bool auto)
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