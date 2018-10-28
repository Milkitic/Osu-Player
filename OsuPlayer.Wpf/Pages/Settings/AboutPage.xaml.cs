using Milkitic.OsuPlayer.Control;
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
using Milkitic.OsuPlayer.Windows;

namespace Milkitic.OsuPlayer.Pages.Settings
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
            CurrentVer.Content = App.Updater.CurrentVersion;
            if (App.Updater.NewRelease != null)
                NewVersion.Visibility = Visibility.Visible;
            GetLastUpdate();
        }

        private void GetLastUpdate()
        {
            LastUpdate.Content = App.Config.LastUpdateCheck == null
                ? "从未"
                : App.Config.LastUpdateCheck.Value.ToString(_dtFormat);
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            //todo: action
            CheckUpdate.IsEnabled = false;
            var b = await App.Updater.CheckUpdateAsync();
            CheckUpdate.IsEnabled = true;
            if (b == null)
            {
                MsgBox.Show(_configWindow, "检查更新时出错。", _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            App.Config.LastUpdateCheck = DateTime.Now;
            GetLastUpdate();
            App.SaveConfig();
            if (b.Value)
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
            _newVersionWindow = new NewVersionWindow(App.Updater.NewRelease, _mainWindow);
            _newVersionWindow.ShowDialog();
        }
    }
}
