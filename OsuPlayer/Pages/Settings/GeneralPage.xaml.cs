using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Common.Instances;
using Milki.OsuPlayer.Common.Scanning;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Presentation;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// GeneralPage.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly ConfigWindow _configWindow;
        private FileScannerViewModel ScannerViewModel { get; }
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public GeneralPage()
        {
            _mainWindow = WindowEx.GetCurrentFirst<MainWindow>();
            _configWindow = WindowEx.GetCurrentFirst<ConfigWindow>();
            InitializeComponent();
            ScannerViewModel = Service.Get<OsuFileScanner>().ViewModel;
            DataContext = ScannerViewModel;
        }

        private void RunOnStartup_CheckChanged(object sender, RoutedEventArgs e)
        {
            var isRunOnStartup = RunOnStartup.IsChecked == true;

            using (var rKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (isRunOnStartup)
                {
                    rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule?.FileName ?? "");
                    AppSettings.Default.GeneralSection.RunOnStartup = true;
                }
                else
                {
                    rKey?.DeleteValue("OsuPlayer", false);
                    AppSettings.Default.GeneralSection.RunOnStartup = false;
                }
            }

            AppSettings.SaveDefault();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnStartup.IsChecked = AppSettings.Default.GeneralSection.RunOnStartup;
            TbDbPath.Text = AppSettings.Default.GeneralSection.DbPath;
            TbCustomPath.Text = AppSettings.Default.GeneralSection.CustomSongsPath;

            if (AppSettings.Default.GeneralSection.ExitWhenClosed.HasValue)
            {
                if (AppSettings.Default.GeneralSection.ExitWhenClosed.Value)
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
                AppSettings.Default.GeneralSection.ExitWhenClosed = true;
            else if (RadioMinimum.IsChecked.HasValue && RadioMinimum.IsChecked.Value)
                AppSettings.Default.GeneralSection.ExitWhenClosed = false;

            AsDefault.IsChecked = true;
            AppSettings.SaveDefault();
        }

        private async void BrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var result = CommonUtils.BrowseDb(out var path);
            if (!result.HasValue || !result.Value)
                return;
            try
            {
                await Service.Get<OsuDbInst>().SyncOsuDbAsync(path, false);
                TbDbPath.Text = path;
                AppSettings.Default.GeneralSection.DbPath = path;
                AppSettings.SaveDefault();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while syncing osu!db: {0}", path);
                MessageBox.Show(_configWindow, string.Format("{0}: {1}\r\n{2}",
                        I18NUtil.GetString("err-osudb-sync"), path, ex.Message),
                    _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AsDefault_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (AsDefault.IsChecked.HasValue && !AsDefault.IsChecked.Value)
                AppSettings.Default.GeneralSection.ExitWhenClosed = null;
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
                    AppSettings.Default.GeneralSection.CustomSongsPath = path;
                    AppSettings.SaveDefault();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while scanning custom folder: {0}", path);
                    MessageBox.Show(_configWindow, string.Format("{0}: {1}\r\n{2}",
                            I18NUtil.GetString("err-custom-scan"), path, ex.Message),
                        _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
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
                await Service.Get<OsuDbInst>().SyncOsuDbAsync(AppSettings.Default.GeneralSection.DbPath, false);
                AppSettings.Default.LastTimeScanOsuDb = DateTime.Now;
                AppSettings.SaveDefault();
            }
            catch (Exception ex)
            {
                var path = AppSettings.Default.GeneralSection.DbPath;
                Logger.Error(ex, "Error while scanning custom folder: {0}", path);
                MessageBox.Show(_configWindow, string.Format("{0}: {1}\r\n{2}",
                        I18NUtil.GetString("err-custom-scan"), path, ex.Message),
                    _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ScanNow_Click(object sender, RoutedEventArgs e)
        {
            await Service.Get<OsuFileScanner>().CancelTaskAsync();
            await Service.Get<OsuFileScanner>().NewScanAndAddAsync(AppSettings.Default.GeneralSection.CustomSongsPath);
        }
    }
}
