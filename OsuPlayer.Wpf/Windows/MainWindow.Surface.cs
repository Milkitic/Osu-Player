using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Instances;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
        /// <summary>
        /// Update collections in the navigation bar.
        /// </summary>
        public void UpdateCollections()
        {
            var list = (List<Collection>)DbOperate.GetCollections();
            list.Reverse();
            ViewModel.Collection = list;
        }

        private bool SetFaved(MapIdentity identity)
        {
            var map = DbOperate.GetMapFromDb(identity);
            var album = DbOperate.GetCollectionsByMap(map);
            bool faved = album != null && album.Any(k => k.Locked);
            BtnLike.Background = faved
                ? (ViewModel.IsMiniMode
                    ? (Brush)ToolControl.FindResource("FavedS")
                    : (Brush)ToolControl.FindResource("Faved"))
                : (ViewModel.IsMiniMode
                    ? (Brush)ToolControl.FindResource("FavS")
                    : (Brush)ToolControl.FindResource("Fav"));
            return faved;
        }

        /// <summary>
        /// Call lyric provider to check lyric
        /// todo: this should run synchronously.
        /// </summary>
        public void SetLyric()
        {
            if (!LyricWindow.IsVisible) return;
            if (InstanceManage.GetInstance<PlayersInst>().AudioPlayer == null) return;
            var lyric = InstanceManage.GetInstance<LyricsInst>().LyricProvider.GetLyric(InstanceManage.GetInstance<PlayersInst>().AudioPlayer.OsuFile.Metadata.ArtistMeta.ToUnicodeString(),
                InstanceManage.GetInstance<PlayersInst>().AudioPlayer.OsuFile.Metadata.TitleMeta.ToUnicodeString(), InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Duration);
            LyricWindow.SetNewLyric(lyric, InstanceManage.GetInstance<PlayersInst>().AudioPlayer.OsuFile);
            LyricWindow.StartWork();
        }

        /// <summary>
        /// Initialize default player settings.
        /// </summary>
        private void LoadSurfaceSettings()
        {
            MasterVolume.Value = PlayerConfig.Current.Volume.Main * 100;
            MusicVolume.Value = PlayerConfig.Current.Volume.Music * 100;
            HitsoundVolume.Value = PlayerConfig.Current.Volume.Hitsound * 100;
        }

        /// <summary>
        /// Start a task to update player info (Starter).
        /// </summary>
        private void RunSurfaceUpdate()
        {
            _statusTask = Task.Run(new Action(UpdateSurface), _cts.Token);
        }

        /// <summary>
        /// Start a task to update player info (Looping in another thread).
        /// </summary>
        private void UpdateSurface()
        {
            return;
            const int interval = 500;
            while (!_cts.IsCancellationRequested)
            {
                if (InstanceManage.GetInstance<PlayersInst>().AudioPlayer == null)
                {
                    Thread.Sleep(interval);
                    continue;
                }

                if (_tmpStatus != InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayerStatus)
                {
                    var s = InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayerStatus;
                    switch (s)
                    {
                        case PlayerStatus.Playing:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ViewModel.IsPlaying = true;
                                ((ContentPresenter)LyricWindow.BtnPlay.Content).Content =
                                    LyricWindow.MainGrid.FindResource("PauseButton");
                                BtnPlay.Style = (Style)FindResource("PauseButtonStyle");
                            }));
                            break;
                        case PlayerStatus.Finished:
                            Dispatcher.BeginInvoke(new Action(() => { PlayNext(false, true); }));
                            break;
                        case PlayerStatus.Stopped:
                        case PlayerStatus.Paused:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (InstanceManage.GetInstance<PlayersInst>().AudioPlayer == null) return;
                                var playTime = Math.Min(InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayTime, PlayProgress.Maximum);
                                ViewModel.IsPlaying = false;
                                ((ContentPresenter)LyricWindow.BtnPlay.Content).Content =
                                    LyricWindow.MainGrid.FindResource("PlayButton");
                                BtnPlay.Style = (Style)FindResource("PlayButtonStyle");
                                PlayProgress.Value = playTime < 0 ? 0 : playTime;
                                LblTotal.Content =
                                    new TimeSpan(0, 0, 0, 0, InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Duration).ToString(@"mm\:ss");
                                LblNow.Content =
                                    new TimeSpan(0, 0, 0, 0, InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayTime).ToString(@"mm\:ss");
                            }));
                            break;
                    }

                    _tmpStatus = InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayerStatus;
                }

                if (_tmpStatus == PlayerStatus.Playing && !_scrollLock)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (InstanceManage.GetInstance<PlayersInst>().AudioPlayer == null) return;
                        var playTime = Math.Min(InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayTime, PlayProgress.Maximum);
                        PlayProgress.Maximum = InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Duration;
                        PlayProgress.Value = playTime < 0
                            ? 0
                            : (playTime > PlayProgress.Maximum ? PlayProgress.Maximum : playTime);
                        LblTotal.Content = new TimeSpan(0, 0, 0, 0, InstanceManage.GetInstance<PlayersInst>().AudioPlayer.Duration).ToString(@"mm\:ss");
                        LblNow.Content = new TimeSpan(0, 0, 0, 0, InstanceManage.GetInstance<PlayersInst>().AudioPlayer.PlayTime).ToString(@"mm\:ss");
                    }));
                }

                Thread.Sleep(interval);
            }
        }

        private void ToMiniMode()
        {
            ViewModel.IsMiniMode = true;
            MinHeight = 48 + 38 + 2;
            Height = MinHeight;
            MinWidth = 360 + 38;
            Width = MinWidth;
            Topmost = true;
            BtnPrev.Margin = new Thickness(5);
            BtnPlay.Margin = new Thickness(5);
            BtnNext.Margin = new Thickness(5);
            BtnMode.Margin = new Thickness(2);
            BtnLike.Margin = new Thickness(2);
            BtnVolume.Margin = new Thickness(2);
            LblArtist.Margin = new Thickness(0, -2, 0, -2);
            LblTitle.Margin = new Thickness(0, -2, 0, -2);

            BtnLike.Width = BtnLike.Height = 18;
            BtnVolume.Width = BtnVolume.Height = 18;
            BtnMode.Width = BtnMode.Height = 18;

            LblMeta.Orientation = Orientation.Vertical;
            FuncPanel.Orientation = Orientation.Vertical;

            BtnMax.Visibility = Visibility.Visible;
            BorderMini.Visibility = Visibility.Visible;
            // modoules
            BtnOpen.Visibility = Visibility.Collapsed;
            ThumbArea.Visibility = Visibility.Collapsed;
            Grip.Visibility = Visibility.Collapsed;
            LblProgress.Visibility = Visibility.Collapsed;
            LblSeperate.Visibility = Visibility.Collapsed;
            PlayProgress.Visibility = Visibility.Collapsed;
            //area
            TitleBarArea.Visibility = Visibility.Collapsed;
            TitleBarAreaBack.Visibility = Visibility.Collapsed;
            MainFrame.Visibility = Visibility.Collapsed;

            SetFaved(InstanceManage.GetInstance<PlayerList>().CurrentInfo.Identity);
            SetPlayMode(InstanceManage.GetInstance<PlayerList>().PlayerMode);
        }

        private void ToNormalMode()
        {
            ViewModel.IsMiniMode = false;
            MinHeight = 100 + 38;
            Height = 720 + 38;
            MinWidth = 840 + 38;
            Width = 960 + 38;
            BtnPrev.Margin = new Thickness(8);
            BtnPlay.Margin = new Thickness(8);
            BtnNext.Margin = new Thickness(8);
            BtnMode.Margin = new Thickness(8);
            BtnLike.Margin = new Thickness(8);
            BtnVolume.Margin = new Thickness(8);
            LblArtist.Margin = new Thickness(0);
            LblTitle.Margin = new Thickness(0);

            BtnLike.Width = BtnLike.Height = 24;
            BtnVolume.Width = BtnVolume.Height = 24;
            BtnMode.Width = BtnMode.Height = 24;

            LblMeta.Orientation = Orientation.Horizontal;
            FuncPanel.Orientation = Orientation.Horizontal;

            BtnMax.Visibility = Visibility.Collapsed;
            BorderMini.Visibility = Visibility.Hidden;

            // modoules
            BtnOpen.Visibility = Visibility.Visible;
            ThumbArea.Visibility = Visibility.Visible;
            Grip.Visibility = Visibility.Visible;
            LblProgress.Visibility = Visibility.Visible;
            LblSeperate.Visibility = Visibility.Visible;
            PlayProgress.Visibility = Visibility.Visible;
            //area
            TitleBarArea.Visibility = Visibility.Visible;
            TitleBarAreaBack.Visibility = Visibility.Visible;
            MainFrame.Visibility = Visibility.Visible;

            SetFaved(InstanceManage.GetInstance<PlayerList>().CurrentInfo.Identity);
            SetPlayMode(InstanceManage.GetInstance<PlayerList>().PlayerMode);

            Topmost = false;
        }
    }
}
