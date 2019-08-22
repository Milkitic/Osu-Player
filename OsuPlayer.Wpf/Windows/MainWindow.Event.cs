using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Control.Notification;
using BeatmapDbOperator = Milky.OsuPlayer.Common.Data.EF.BeatmapDbOperator;

namespace Milky.OsuPlayer.Windows
{
    partial class MainWindow
    {
        private static BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();
        private AppDbOperator _appDbOperator = new AppDbOperator();
        #region Window events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.NotificationList = new ObservableCollection<NotificationOption>();
            Notification.ItemsSource = App.NotificationList;
            if (AppSettings.Current.General.FirstOpen)
            {
                WelcomeControl.Show();
                await Services.Get<OsuDbInst>().LoadLocalDbAsync();
            }
            else
            {
                await Services.Get<OsuFileScanner>().NewScanAndAddAsync(AppSettings.Current.General.CustomSongsPath);
                if (DateTime.Now - AppSettings.Current.LastTimeScanOsuDb > TimeSpan.FromDays(1))
                {
                    await Services.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Current.General.DbPath, true);
                    AppSettings.Current.LastTimeScanOsuDb = DateTime.Now;
                    AppSettings.SaveCurrent();
                }
            }

            UpdateCollections();

            if (AppSettings.Current.CurrentPath != null && AppSettings.Current.Play.Memory)
            {
                var entries = _beatmapDbOperator.GetBeatmapsByIdentifiable(AppSettings.Current.CurrentList);
                await Services.Get<PlayerList>()
                    .RefreshPlayListAsync(PlayerList.FreshType.All, beatmaps: entries);

                bool play = AppSettings.Current.Play.AutoPlay;
                await PlayController.Default.PlayNewFile(AppSettings.Current.CurrentPath, play);
            }

            PlayController.Default.OnNewFileLoaded += Controller_OnNewFileLoaded;
            PlayController.Default.OnProgressDragComplete += Controller_OnProgressDragComplete;
            PlayController.Default.OnLikeClick += Controller_OnLikeClick;
            PlayController.Default.OnThumbClick += Controller_OnThumbClick;
            PlayController.Default.OnPlayClick += Controller_OnPlayClick;
            PlayController.Default.OnPauseClick += Controller_OnPauseClick;
            var helper = new WindowInteropHelper(this);
            //var source = HwndSource.FromHwnd(helper.Handle);
            //source?.AddHook(HwndMessageHook);

            var updater = Services.Get<Updater>();
            bool? hasUpdate = await updater.CheckUpdateAsync();
            if (hasUpdate == true && updater.NewRelease.NewVerString != AppSettings.Current.IgnoredVer)
            {
                var newVersionWindow = new NewVersionWindow(updater.NewRelease, this);
                newVersionWindow.ShowDialog();
            }
        }

        private static void ScanSynchronously()
        {
            Task.Run(() => Services.Get<OsuFileScanner>().NewScanAndAddAsync(AppSettings.Current.General.CustomSongsPath));
        }

        private static void SyncSynchronously()
        {
            Task.Run(() => Services.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Current.General.DbPath, true));
        }

        /// <summary>
        /// Clear things.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AppSettings.Current.General.ExitWhenClosed == null && !ForceExit)
            {
                e.Cancel = true;
                FramePop.Navigate(new ClosingPage(this));
                return;
            }
            else if (AppSettings.Current.General.ExitWhenClosed == false && !ForceExit)
            {
                WindowState = WindowState.Minimized;
                Hide();
                e.Cancel = true;
                return;
            }

            PlayController.Default?.Dispose();
            LyricWindow.Dispose();
            NotifyIcon.Dispose();
            if (ConfigWindow == null || ConfigWindow.IsClosed)
                return;
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
            App.NotificationList.Add(new NotificationOption
            {
                Title = Title,
                Content = "功能完善中，敬请期待~"
            });
            //bool? b = await PageBox.ShowDialog(Title, "功能完善中，敬请期待~");
        }

        /// <summary>
        /// Navigate storyboard page.
        /// </summary>
        private void Storyboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(Pages.StoryboardPage);
        }

        /// <summary>
        /// Navigate recent page.
        /// </summary>
        private void BtnRecent_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.Content?.GetType() != typeof(RecentPlayPage))
                MainFrame.Navigate(Pages.RecentPlayPage);
        }

        /// <summary>
        /// Navigate export page.
        /// </summary>
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
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
                MainFrame.Navigate(new CollectionPage(this, _appDbOperator.GetCollectionById(colId)));
            if (MainFrame.Content?.GetType() == typeof(CollectionPage))
            {
                var sb = (CollectionPage)MainFrame.Content;
                if (sb.Id != colId)
                    MainFrame.Navigate(new CollectionPage(this, _appDbOperator.GetCollectionById(colId)));
            }
        }

        private void BtnNavigate_Checked(object sender, RoutedEventArgs e)
        {
            if (_ischanging)
                return;
            _ischanging = true;
            var btn = (ToggleButton)sender;
            _optionContainer.Switch(btn);
            _ischanging = false;
        }

        private void BtnNavigate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_ischanging)
                return;
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
            ViewModel.IsMiniMode = true;
        }

        #endregion Title events

        #region Play control events

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
            if (PlayerViewModel.Current.EnableVideo)
            {
                SetFullScr();
                PlayerViewModel.Current.EnableVideo = false;
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

        private async void BtnLike_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion Play control events

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
            if (LyricWindow.IsShown)
            {
                LyricWindow.Hide();
            }
            else
            {
                LyricWindow.Show();
            }
        }

        private void MenuLockLyric_Click(object sender, RoutedEventArgs e)
        {
            LyricWindow.IsLocked = !LyricWindow.IsLocked;
        }

        #endregion Notification events
    }
}
