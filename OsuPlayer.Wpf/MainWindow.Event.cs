using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Media;
using Milkitic.OsuPlayer.Media.Music;
using Milkitic.OsuPlayer.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace Milkitic.OsuPlayer
{
    partial class MainWindow
    {
        #region Window events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //// todo: This should be kept since the application exit last time.
            //BtnRecent_Click(sender, e);
            UpdateCollections();
            LoadSurfaceSettings();
            RunSurfaceUpdate();
            if (App.Config.CurrentPath != null && App.Config.Play.Memory)
            {
                var entries = App.Beatmaps.GetBeatmapByIdentities(App.Config.CurrentList);
                App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, entries: entries);
                bool play = App.Config.Play.AutoPlay;
                PlayNewFile(App.Config.CurrentPath, play);
            }

            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source?.AddHook(HwndMessageHook);

            bool? sb = await App.Updater.CheckUpdateAsync();
            if (sb.HasValue && sb.Value)
            {
                NewVersionWindow newVersionWindow = new NewVersionWindow(App.Updater.NewRelease, this);
                newVersionWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Clear things.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (App.Config.General.ExitWhenClosed == null && !ForceExit)
            {
                e.Cancel = true;
                FramePop.Navigate(new ClosingPage(this));
                return;
            }
            else if (App.Config.General.ExitWhenClosed == false && !ForceExit)
            {
                WindowState = WindowState.Minimized;
                Hide();
                e.Cancel = true;
                return;
            }

            ClearHitsoundPlayer();
            _cts.Dispose();
            WavePlayer.Device?.Dispose();
            WavePlayer.MasteringVoice?.Dispose();
            LyricWindow.Dispose();
            NotifyIcon.Dispose();
            if (ConfigWindow == null || ConfigWindow.IsClosed) return;
            if (ConfigWindow.IsInitialized)
                ConfigWindow.Close();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                return;
            _lastState = WindowState;
        }


        #endregion Window events

        #region Navigation events

        private bool _ischanging = false;

        /// <summary>
        /// Navigate search page.
        /// </summary>
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            if (MainFrame.Content?.GetType() != typeof(SearchPage))
                MainFrame.Navigate(Pages.SearchPage);
            //MainFrame.Navigate(new Uri("Pages/SearchPage.xaml", UriKind.Relative), this);
        }

        /// <summary>
        /// Navigate find page.
        /// </summary>
        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            //MainFrame.Navigate(Pages.FindPage);
            PageBox.Show(Title, "功能完善中，敬请期待~", delegate { });
            //bool? b = await PageBox.ShowDialog(Title, "功能完善中，敬请期待~");
        }

        /// <summary>
        /// Navigate storyboard page.
        /// </summary>
        private void Storyboard_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
#if DEBUG
            MainFrame.Navigate(Pages.StoryboardPage);
#else
            PageBox.Show(Title, "功能完善中，敬请期待~", delegate { });
#endif
        }

        /// <summary>
        /// Navigate recent page.
        /// </summary>
        private void BtnRecent_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            if (MainFrame.Content?.GetType() != typeof(RecentPlayPage))
                MainFrame.Navigate(Pages.RecentPlayPage);
        }

        /// <summary>
        /// Navigate export page.
        /// </summary>
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            if (MainFrame.Content?.GetType() != typeof(ExportPage))
                MainFrame.Navigate(Pages.ExportPage);
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(this));
        }

        /// <summary>
        /// Navigate collection page.
        /// </summary>
        private void BtnCollection_Click(object sender, RoutedEventArgs e)
        {
            var btn = (ToggleButton)sender;
            var colId = (string)btn.Tag;
            if (MainFrame.Content?.GetType() != typeof(CollectionPage))
                MainFrame.Navigate(new CollectionPage(this, DbOperator.GetCollectionById(colId)));
            if (MainFrame.Content?.GetType() == typeof(CollectionPage))
            {
                var sb = (CollectionPage)MainFrame.Content;
                if (sb.Id != colId)
                    MainFrame.Navigate(new CollectionPage(this, DbOperator.GetCollectionById(colId)));
            }
        }

        private void BtnNavigate_Checked(object sender, RoutedEventArgs e)
        {
            if (_ischanging) return;
            _ischanging = true;
            var btn = (ToggleButton)sender;
            _optionContainer.Switch(btn);
            _ischanging = false;
        }

        private void BtnNavigate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_ischanging) return;
            _ischanging = true;
            ((ToggleButton)sender).IsChecked = true;
            _ischanging = false;
        }

        #endregion Navigation events

        #region Title events

        /// <summary>
        /// Popup a dialog for settings.
        /// </summary>
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigWindow == null || ConfigWindow.IsClosed)
            {
                ConfigWindow = new ConfigWindow(this);
                ConfigWindow.Show();
            }
            else
            {
                if (ConfigWindow.IsInitialized)
                    ConfigWindow.Focus();
            }
        }

        private void BtnMini_Click(object sender, RoutedEventArgs e)
        {
            ToMiniMode();
        }

        #endregion Title events

        #region Play control events

        private void ThumbButton_Click(object sender, RoutedEventArgs e)
        {
            if (FullModeArea.Visibility == Visibility.Hidden)
                FullModeArea.Visibility = Visibility.Visible;
            else if (FullModeArea.Visibility == Visibility.Visible)
                FullModeArea.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Play next song in playlist.
        /// </summary>
        public async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (App.HitsoundPlayer == null)
            {
                BtnOpen_Click(sender, e);
                return;
            }

            switch (App.HitsoundPlayer.PlayerStatus)
            {
                case PlayerStatus.Playing:
                    App.HitsoundPlayer.Pause();
                    if (VideoElement.Source != null) await VideoElement.Pause();
                    App.StoryboardProvider?.StoryboardTiming.Pause();
                    break;
                case PlayerStatus.Ready:
                case PlayerStatus.Stopped:
                case PlayerStatus.Paused:
                    App.HitsoundPlayer.Play();
                    if (VideoElement.Source != null) await VideoElement.Play();
                    App.StoryboardProvider?.StoryboardTiming.Start();
                    break;
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            PlayNewFile(LoadFile());
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            PlayNext(true, false);
        }

        public void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            PlayNext(true, true);
        }

        private void BtnMode_Click(object sender, RoutedEventArgs e)
        {
            PopMode.IsOpen = true;
        }

        /// <summary>
        /// Popup a dialog for adding music to a collection.
        /// </summary>
        private void BtnLike_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDb()) return;
            var entry = App.Beatmaps.GetBeatmapByIdentity(App.PlayerList.CurrentIdentity);
            //var entry = App.PlayerList?.CurrentInfo.Entry;
            if (entry == null)
            {
                PageBox.Show(Title, "该图不存在于该osu!db中。", delegate { });
                return;
            }
            if (!_miniMode)
                FramePop.Navigate(new SelectCollectionPage(this, entry));
            else
            {
                var collection = DbOperator.GetCollections().First(k => k.Locked);
                if (App.PlayerList.CurrentInfo.IsFaved)
                {
                    DbOperator.RemoveMapFromCollection(entry, collection);
                    App.PlayerList.CurrentInfo.IsFaved = false;
                }
                else
                {
                    SelectCollectionPage.AddToCollection(collection, entry);
                    App.PlayerList.CurrentInfo.IsFaved = true;
                }
            }

            SetFaved(App.PlayerList.CurrentInfo.Identity);
        }

        private void BtnVolume_Click(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = true;
        }

        private void PlayMode_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (Single.IsChecked == true)
            {
                BtnMode.Content = "单曲播放";
                App.PlayerList.PlayerMode = PlayerMode.Single;
            }
            else if (SingleLoop.IsChecked == true)
            {
                BtnMode.Content = "单曲循环";
                App.PlayerList.PlayerMode = PlayerMode.SingleLoop;
            }
            else if (Normal.IsChecked == true)
            {
                BtnMode.Content = "顺序播放";
                App.PlayerList.PlayerMode = PlayerMode.Normal;
            }
            else if (Random.IsChecked == true)
            {
                BtnMode.Content = "随机播放";
                App.PlayerList.PlayerMode = PlayerMode.Random;
            }
            else if (Loop.IsChecked == true)
            {
                BtnMode.Content = "循环列表";
                App.PlayerList.PlayerMode = PlayerMode.Loop;
            }
            else if (LoopRandom.IsChecked == true)
            {
                BtnMode.Content = "随机循环";
                App.PlayerList.PlayerMode = PlayerMode.LoopRandom;
            }

            App.PlayerList.RefreshPlayList(PlayerList.FreshType.IndexOnly);
        }

        private void BtnMax_Click(object sender, RoutedEventArgs e)
        {
            ToNormalMode();
        }

        private void PlayProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            _scrollLock = true;
        }

        /// <summary>
        /// Play progress control.
        /// While drag started, slider's updating should be recoverd.
        /// </summary>
        private void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (App.HitsoundPlayer != null)
            {
                switch (App.HitsoundPlayer.PlayerStatus)
                {
                    case PlayerStatus.Playing:
                        if (VideoElement.Source != null)
                            VideoElement.Position = new TimeSpan(0, 0, 0, 0, (int)(PlayProgress.Value + _videoOffset));
                        App.HitsoundPlayer.SetTime((int)PlayProgress.Value);
                        App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, true);
                        break;
                    case PlayerStatus.Paused:
                    case PlayerStatus.Stopped:
                        if (VideoElement.Source != null)
                            VideoElement.Position = new TimeSpan(0, 0, 0, 0, (int)(PlayProgress.Value + _videoOffset));
                        App.HitsoundPlayer.SetTime((int)PlayProgress.Value, false);
                        App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, false);
                        break;
                }
            }

            _scrollLock = false;
        }

        private void Mod_CheckChanged(object sender, RoutedEventArgs e)
        {
            PlayMod mod;
            if (ModNone.IsChecked == true)
                mod = PlayMod.None;
            else if (ModDt.IsChecked == true)
                mod = PlayMod.DoubleTime;
            else if (ModHt.IsChecked == true)
                mod = PlayMod.HalfTime;
            else if (ModNc.IsChecked == true)
                mod = PlayMod.NightCore;
            else if (ModDc.IsChecked == true)
                mod = PlayMod.DayCore;
            else
                throw new ArgumentOutOfRangeException();

            App.Config.Play.PlayMod = mod;
            App.MusicPlayer.SetPlayMod(mod);
            App.HitsoundPlayer.SetPlayMod(mod, App.HitsoundPlayer.PlayerStatus == PlayerStatus.Playing);
        }

        #endregion Play control events

        #region Popup events

        /// <summary>
        /// While popup lost focus, we should hide it.
        /// </summary>
        private void Popup_LostFocus(object sender, RoutedEventArgs e)
        {
            Pop.IsOpen = false;
            PopMode.IsOpen = false;
        }

        /// <summary>
        /// Play progress control.
        /// While drag started, slider's updating should be paused.
        /// </summary>

        /// <summary>
        /// Master Volume Settings
        /// </summary>
        private void MasterVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Main = (float)(MasterVolume.Value / 100);
        }

        /// <summary>
        /// Music Volume Settings
        /// </summary>
        private void MusicVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Music = (float)(MusicVolume.Value / 100);
        }

        /// <summary>
        /// Effect Volume Settings
        /// </summary>
        private void HitsoundVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Hitsound = (float)(HitsoundVolume.Value / 100);
        }

        /// <summary>
        /// Offset Settings
        /// </summary>
        private void Offset_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (App.HitsoundPlayer == null) return;
            App.HitsoundPlayer.SingleOffset = (int)Offset.Value;
            DbOperator.UpdateMap(App.PlayerList.CurrentInfo.Identity, App.HitsoundPlayer.SingleOffset);
        }

        #endregion Popup events

        #region Notification events

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Topmost = true;
            Topmost = false;
            Show();
            WindowState = _lastState;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            ForceExit = true;
            this.Close();
        }

        private void MenuConfig_Click(object sender, RoutedEventArgs e)
        {
            BtnSettings_Click(sender, e);
        }

        #endregion Notification events

        #region Video element events

        private async void VideoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Visible;
            await VideoElement.Play();
        }

        private void VideoElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Hidden;
            PageBox.Show("不支持的视频格式", e.ErrorException.ToString(), () => { });
        }

        #endregion Video element events
    }
}
