using Microsoft.Win32;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Windows;
using osu_database_reader.BinaryFiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                App.Config.General.RunOnStartup = true;
            }
            else
            {
                rKey?.DeleteValue("OsuPlayer", false);
                App.Config.General.RunOnStartup = false;
            }

            App.SaveConfig();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RunOnStartup.IsChecked = App.Config.General.RunOnStartup;
            LblDbPath.Text = App.Config.General.DbPath;
            if (App.Config.General.ExitWhenClosed.HasValue)
            {
                if (App.Config.General.ExitWhenClosed.Value)
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
                App.Config.General.ExitWhenClosed = true;
            else if (RadioMinimum.IsChecked.HasValue && RadioMinimum.IsChecked.Value)
                App.Config.General.ExitWhenClosed = false;

            AsDefault.IsChecked = true;
            App.SaveConfig();
        }

        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            var result = App.BrowseDb(out var path);
            if (!result.HasValue || !result.Value)
                return;
            try
            {
                await BeatmapEntryQuery.LoadNewDbAsync(path);
            }
            catch (Exception ex)
            {
                MsgBox.Show(_configWindow, ex.Message, _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AsDefault_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (AsDefault.IsChecked.HasValue && !AsDefault.IsChecked.Value)
                App.Config.General.ExitWhenClosed = null;
            else
                Radio_CheckChanged(sender, e);
            App.SaveConfig();
        }
    }
}
