using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Scanning;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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

        public GeneralPage(MainWindow mainWindow, ConfigWindow configWindow)
        {
            _mainWindow = mainWindow;
            _configWindow = configWindow;
            InitializeComponent();
            ScannerViewModel = Services.Get<OsuFileScanner>().ViewModel;
        }

        private void RunOnStartup_CheckChanged(object sender, RoutedEventArgs e)
        {
            var isRunOnStartup = RunOnStartup.IsChecked == true;

            using (var rKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (isRunOnStartup)
                {
                    rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule.FileName);
                    PlayerConfig.Current.General.RunOnStartup = true;
                }
                else
                {
                    rKey?.DeleteValue("OsuPlayer", false);
                    PlayerConfig.Current.General.RunOnStartup = false;
                }
            }

            PlayerConfig.SaveCurrent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnStartup.IsChecked = PlayerConfig.Current.General.RunOnStartup;
            TbDbPath.Text = PlayerConfig.Current.General.DbPath;
            TbCustomPath.Text = PlayerConfig.Current.General.CustomSongsPath;

            if (PlayerConfig.Current.General.ExitWhenClosed.HasValue)
            {
                if (PlayerConfig.Current.General.ExitWhenClosed.Value)
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
                PlayerConfig.Current.General.ExitWhenClosed = true;
            else if (RadioMinimum.IsChecked.HasValue && RadioMinimum.IsChecked.Value)
                PlayerConfig.Current.General.ExitWhenClosed = false;

            AsDefault.IsChecked = true;
            PlayerConfig.SaveCurrent();
        }

        private async void BrowseDb_Click(object sender, RoutedEventArgs e)
        {
            var result = Util.BrowseDb(out var path);
            if (!result.HasValue || !result.Value)
                return;
            try
            {
                await Services.Get<OsuDbInst>().SyncOsuDbAsync(path, false);
                TbDbPath.Text = path;
                PlayerConfig.Current.General.DbPath = path;
                PlayerConfig.SaveCurrent();
            }
            catch (Exception ex)
            {
                MsgBox.Show(_configWindow, ex.Message, _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AsDefault_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (AsDefault.IsChecked.HasValue && !AsDefault.IsChecked.Value)
                PlayerConfig.Current.General.ExitWhenClosed = null;
            else
                Radio_CheckChanged(sender, e);
            PlayerConfig.SaveCurrent();
        }

        private async void BrowseCustom_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Folder"
            };

            var result = openFileDialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok)
                return;
            var path = openFileDialog.FileName;
            try
            {
                TbCustomPath.Text = path;
                await Services.Get<OsuFileScanner>().CancelTaskAsync();
                await Services.Get<OsuFileScanner>().NewScanAndAddAsync(path);
                PlayerConfig.Current.General.CustomSongsPath = path;
                PlayerConfig.SaveCurrent();
            }
            catch (Exception ex)
            {
                MsgBox.Show(_configWindow, ex.Message, _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CancelScan_Click(object sender, RoutedEventArgs e)
        {
            await Services.Get<OsuFileScanner>().CancelTaskAsync();
        }
    }
}
