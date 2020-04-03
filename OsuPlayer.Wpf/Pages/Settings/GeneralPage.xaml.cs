using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.UiComponent.NotificationComponent;

namespace Milky.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// GeneralPage.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly ConfigWindow _configWindow;
        private FileScannerViewModel ScannerViewModel { get; }

        public GeneralPage()
        {
            _mainWindow = WindowEx.GetCurrentFirst<MainWindow>();
            _configWindow = WindowEx.GetCurrentFirst<ConfigWindow>();
            InitializeComponent();
            ScannerViewModel = Service.Get<OsuFileScanner>().ViewModel;
        }

        private void RunOnStartup_CheckChanged(object sender, RoutedEventArgs e)
        {
            var isRunOnStartup = RunOnStartup.IsChecked == true;

            using (var rKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (isRunOnStartup)
                {
                    rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule?.FileName ?? "");
                    AppSettings.Default.General.RunOnStartup = true;
                }
                else
                {
                    rKey?.DeleteValue("OsuPlayer", false);
                    AppSettings.Default.General.RunOnStartup = false;
                }
            }

            AppSettings.SaveDefault();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnStartup.IsChecked = AppSettings.Default.General.RunOnStartup;
            TbDbPath.Text = AppSettings.Default.General.DbPath;
            TbCustomPath.Text = AppSettings.Default.General.CustomSongsPath;

            if (AppSettings.Default.General.ExitWhenClosed.HasValue)
            {
                if (AppSettings.Default.General.ExitWhenClosed.Value)
                    RadioExit.IsChecked = true;
                else
                    RadioMinimum.IsChecked = true;
            }
            else
            {
                RadioMinimum.IsChecked = true;
                AsDefault.IsChecked = false;
            }
        }

        private void Radio_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (RadioExit.IsChecked.HasValue && RadioExit.IsChecked.Value)
                AppSettings.Default.General.ExitWhenClosed = true;
            else if (RadioMinimum.IsChecked.HasValue && RadioMinimum.IsChecked.Value)
                AppSettings.Default.General.ExitWhenClosed = false;

            AsDefault.IsChecked = true;
            AppSettings.SaveDefault();
        }

        private async void BrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var result = Util.BrowseDb(out var path);
            if (!result.HasValue || !result.Value)
                return;
            try
            {
                await Service.Get<OsuDbInst>().SyncOsuDbAsync(path, false);
                TbDbPath.Text = path;
                AppSettings.Default.General.DbPath = path;
                AppSettings.SaveDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show(_configWindow, ex.Message, _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AsDefault_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (AsDefault.IsChecked.HasValue && !AsDefault.IsChecked.Value)
                AppSettings.Default.General.ExitWhenClosed = null;
            else
                Radio_CheckChanged(sender, e);
            AppSettings.SaveDefault();
        }

        private async void BrowseCustom_Click(object sender, RoutedEventArgs e)
        {
            using (var openFileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Folder"
            })
            {
                var result = openFileDialog.ShowDialog();
                if (result != CommonFileDialogResult.Ok)
                    return;
                var path = openFileDialog.FileName;
                try
                {
                    TbCustomPath.Text = path;
                    await Service.Get<OsuFileScanner>().CancelTaskAsync();
                    await Service.Get<OsuFileScanner>().NewScanAndAddAsync(path);
                    AppSettings.Default.General.CustomSongsPath = path;
                    AppSettings.SaveDefault();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(_configWindow, ex.Message, _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CancelScan_Click(object sender, RoutedEventArgs e)
        {
            await Service.Get<OsuFileScanner>().CancelTaskAsync();
        }

        private async void SyncNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Service.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Default.General.DbPath, false);
                AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                AppSettings.SaveDefault();
            }
            catch (Exception ex)
            {
                Notification.Push(ex.Message);
            }
        }
    }
}
