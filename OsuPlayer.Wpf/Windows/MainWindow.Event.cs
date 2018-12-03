using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Media;
using Milkitic.OsuPlayer.Media.Music;
using Milkitic.OsuPlayer.Pages;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace Milkitic.OsuPlayer.Windows
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

            SetPlayMode(App.Config.Play.PlayListMode);

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
            _sbWindow?.Close();
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
            MsgBox.Show(this, "功能完善中，敬请期待~", Title, MessageBoxButton.OK, MessageBoxImage.Information);
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
            MsgBox.Show(this, "功能完善中，敬请期待~", Title, MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (!ViewModel.EnableVideo)
                ViewModel.EnableVideo = true;
            else if (ViewModel.EnableVideo)
            {
                if (ResizableArea.Margin == new Thickness(5))
                    SetFullScr();
                else
                    SetFullScrMini();
            }
        }

        private void SetFullScrMini()
        {
            ResizableArea.BorderBrush = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0));
            ResizableArea.BorderThickness = new Thickness(1);
            ResizableArea.HorizontalAlignment = HorizontalAlignment.Right;
            ResizableArea.VerticalAlignment = VerticalAlignment.Bottom;
            ResizableArea.Width = 318;
            ResizableArea.Height = 180;
            ResizableArea.Margin = new Thickness(5);
        }

        private void BtnHideFullScr_Click(object sender, RoutedEventArgs e)
        {
            //if (FullModeArea.Visibility == Visibility.Visible)
            //{
            //    SetFullScr();
            //    FullModeArea.Visibility = Visibility.Hidden;
            //}
            if (ViewModel.EnableVideo)
            {
                SetFullScr();
                ViewModel.EnableVideo = false;
            }
        }

        private void SetFullScr()
        {
            ResizableArea.ClearValue(Border.BorderBrushProperty);
            ResizableArea.ClearValue(Border.BorderThicknessProperty);
            ResizableArea.ClearValue(Border.HorizontalAlignmentProperty);
            ResizableArea.ClearValue(Border.VerticalAlignmentProperty);
            ResizableArea.ClearValue(Border.WidthProperty);
            ResizableArea.ClearValue(Border.HeightProperty);
            ResizableArea.ClearValue(Border.MarginProperty);
        }

        /// <summary>
        /// Play next song in playlist.
        /// </summary>
        public async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            _videoPlay = true;
            _forcePaused = false;
            if (App.HitsoundPlayer == null)
            {
                BtnOpen_Click(sender, e);
                return;
            }

            switch (App.HitsoundPlayer.PlayerStatus)
            {
                case PlayerStatus.Playing:
                    if (VideoElement?.Source != null) await VideoElement.Pause();
                    PauseMedia();
                    break;
                case PlayerStatus.Ready:
                case PlayerStatus.Stopped:
                case PlayerStatus.Paused:
                    if (VideoElement?.Source != null) await VideoElement.Play();
                    PlayMedia();
                    break;
            }
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            PlayNewFile(LoadFile());
        }

        public void BtnPrev_Click(object sender, RoutedEventArgs e)
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
                MsgBox.Show(this, "该图不存在于该osu!db中。", Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!ViewModel.IsMiniMode)
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

        private void PlayMode_Checked(object sender, RoutedEventArgs e)
        {
            if (_ischanging) return;
            _ischanging = true;
            var btn = (ToggleButton)sender;
            _modeOptionContainer.Switch(btn);
            _ischanging = false;
        }

        private void PlayMode_UnChecked(object sender, RoutedEventArgs e)
        {
            if (_ischanging) return;
            _ischanging = true;
            ((ToggleButton)sender).IsChecked = true;
            _ischanging = false;
        }
        private void PlayMode_Click(object sender, RoutedEventArgs e)
        {
            var btn = (ToggleButton)sender;
            PlayerMode playmode;
            switch (btn.Name)
            {
                case "Single":
                    BtnMode.Content = "单曲播放";
                    playmode = PlayerMode.Single;
                    break;
                case "SingleLoop":
                    BtnMode.Content = "单曲循环";
                    playmode = PlayerMode.SingleLoop;
                    break;
                case "Normal":
                    BtnMode.Content = "顺序播放";
                    playmode = PlayerMode.Normal;
                    break;
                case "Random":
                    BtnMode.Content = "随机播放";
                    playmode = PlayerMode.Random;
                    break;
                case "Loop":
                    BtnMode.Content = "循环列表";
                    playmode = PlayerMode.Loop;
                    break;
                default:
                case "LoopRandom":
                    BtnMode.Content = "随机循环";
                    playmode = PlayerMode.LoopRandom;
                    break;
            }

            SetPlayMode(playmode);
            PopMode.IsOpen = false;
        }

        private void SetPlayMode(PlayerMode playmode)
        {
            switch (playmode)
            {
                case PlayerMode.Normal:
                    Normal.IsChecked = true;
                    break;
                case PlayerMode.Random:
                    Random.IsChecked = true;
                    break;
                case PlayerMode.Loop:
                    Loop.IsChecked = true;
                    break;
                case PlayerMode.LoopRandom:
                    LoopRandom.IsChecked = true;
                    break;
                case PlayerMode.Single:
                    Single.IsChecked = true;
                    break;
                case PlayerMode.SingleLoop:
                    SingleLoop.IsChecked = true;
                    break;
            }

            string flag = ViewModel.IsMiniMode ? "S" : "";
            BtnMode.Background = (ImageBrush)ToolControl.FindResource(playmode + flag);
            if (playmode == App.PlayerList.PlayerMode)
                return;
            App.PlayerList.PlayerMode = playmode;
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.IndexOnly);
            App.Config.Play.PlayListMode = playmode;
            App.SaveConfig();
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
        private async void PlayProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (App.HitsoundPlayer != null)
            {
                switch (App.HitsoundPlayer.PlayerStatus)
                {
                    case PlayerStatus.Playing:
                        if (VideoElement.Source != null)
                        {
                            App.HitsoundPlayer.SetTime((int)PlayProgress.Value, false);
                            App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, false);
                            _forcePaused = false;
                            await VideoJumpTo((int)PlayProgress.Value);
                        }
                        else
                        {
                            App.HitsoundPlayer.SetTime((int)PlayProgress.Value);
                            App.StoryboardProvider?.StoryboardTiming.SetTiming((int)PlayProgress.Value, true);
                        }
                        break;
                    case PlayerStatus.Paused:
                    case PlayerStatus.Stopped:
                        _forcePaused = true;
                        if (VideoElement.Source != null)
                            await VideoJumpTo((int)PlayProgress.Value);
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
        /// Play progress control.
        /// While drag started, slider's updating should be paused.
        /// </summary>
        /// <summary>
        /// Master Volume Settings
        /// </summary>
        private void MasterVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Main = (float)(MasterVolume.Value / 100);
            App.SaveConfig();
        }

        /// <summary>
        /// Music Volume Settings
        /// </summary>
        private void MusicVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Music = (float)(MusicVolume.Value / 100);
            App.SaveConfig();
        }

        /// <summary>
        /// Effect Volume Settings
        /// </summary>
        private void HitsoundVolume_DragDelta(object sender, DragDeltaEventArgs e)
        {
            App.Config.Volume.Hitsound = (float)(HitsoundVolume.Value / 100);
            App.SaveConfig();
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


        private void MenuOpenHideLyric_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsLyricWindowShown)
            {
                App.Config.Lyric.EnableLyric = false;
                LyricWindow.Hide();
            }
            else
            {
                App.Config.Lyric.EnableLyric = true;
                LyricWindow.Show();
            }
        }

        private void MenuLockLyric_Click(object sender, RoutedEventArgs e)
        {
            LyricWindow.IsLocked = !LyricWindow.IsLocked;
        }

        #endregion Notification events

        #region Video element events

        private async void VideoElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Visible;
            if (!_videoPlay) return;
            await Task.Run(() => _waitAction?.Invoke());
            await VideoElement.Play();
            VideoElement.Position = _position;
        }

        private async void VideoElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VideoElementBorder.Visibility = Visibility.Hidden;
            //MsgBox.Show(this, e.ErrorException.ToString(), "不支持的视频格式", MessageBoxButton.OK, MessageBoxImage.Error);
            if (!_videoPlay) return;
            await ClearVideoElement(false);
            PlayMedia();
        }

        #endregion Video element events


    }
}
