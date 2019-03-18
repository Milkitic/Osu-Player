using Microsoft.Win32;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Windows;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Instances;

namespace Milky.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// GeneralPage.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly ConfigWindow _configWindow;

        public GeneralPage(MainWindow mainWindow, ConfigWindow configWindow)
        {
            _mainWindow = mainWindow;
            _configWindow = configWindow;
            InitializeComponent();
        }

        private void RunOnStartup_CheckChanged(object sender, RoutedEventArgs e)
        {
            RegistryKey rKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (RunOnStartup.IsChecked.HasValue && RunOnStartup.IsChecked.Value)
            {
                rKey?.SetValue("OsuPlayer", Process.GetCurrentProcess().MainModule.FileName);
                PlayerConfig.Current.General.RunOnStartup = true;
            }
            else
            {
                rKey?.DeleteValue("OsuPlayer", false);
                PlayerConfig.Current.General.RunOnStartup = false;
            }

            PlayerConfig.SaveCurrent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnStartup.IsChecked = PlayerConfig.Current.General.RunOnStartup;
            LblDbPath.Text = PlayerConfig.Current.General.DbPath;
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

        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            var result = App.BrowseDb(out var path);
            if (!result.HasValue || !result.Value)
                return;
            try
            {
                await InstanceManage.GetInstance<OsuDbInst>().LoadNewDbAsync(path);
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
    }
}
