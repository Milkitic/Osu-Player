using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
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
    /// AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class AboutPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly ConfigWindow _configWindow;
        private readonly string _dtFormat = "g";
        private NewVersionWindow _newVersionWindow;

        public AboutPage(MainWindow mainWindow, ConfigWindow configWindow)
        {
            _mainWindow = mainWindow;
            _configWindow = configWindow;
            InitializeComponent();
        }

        private void LinkGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Milkitic/Osu-Player");
        }

        private void LinkFeedback_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Milkitic/Osu-Player/issues/new");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentVer.Content = InstanceManage.GetInstance<Updater>().CurrentVersion;
            if (InstanceManage.GetInstance<Updater>().NewRelease != null)
                NewVersion.Visibility = Visibility.Visible;
            GetLastUpdate();
        }

        private void GetLastUpdate()
        {
            LastUpdate.Content = PlayerConfig.Current.LastUpdateCheck == null
                ? "从未"
                : PlayerConfig.Current.LastUpdateCheck.Value.ToString(_dtFormat);
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            //todo: action
            CheckUpdate.IsEnabled = false;
            var hasNew = await InstanceManage.GetInstance<Updater>().CheckUpdateAsync();
            CheckUpdate.IsEnabled = true;
            if (hasNew == null)
            {
                MsgBox.Show(_configWindow, "检查更新时出错。", _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PlayerConfig.Current.LastUpdateCheck = DateTime.Now;
            GetLastUpdate();
            PlayerConfig.SaveCurrent();
            if (hasNew == true)
            {
                NewVersion.Visibility = Visibility.Visible;
                NewVersion_Click(sender, e);
            }
            else
            {
                MsgBox.Show(_configWindow, "已是最新版本。", _configWindow.Title, MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void NewVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_newVersionWindow != null && !_newVersionWindow.IsClosed)
                _newVersionWindow.Close();
            _newVersionWindow = new NewVersionWindow(InstanceManage.GetInstance<Updater>().NewRelease, _mainWindow);
            _newVersionWindow.ShowDialog();
        }

        private void LinkLicense_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Milkitic/Osu-Player/blob/master/LICENSE");
        }

        private void LinkPrivacy_Click(object sender, RoutedEventArgs e)
        {
            MsgBox.Show("This software will NOT collect any user information.");
        }
    }
}
