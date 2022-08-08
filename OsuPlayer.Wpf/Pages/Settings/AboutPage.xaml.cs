using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Common.Configuration;
using Milki.OsuPlayer.Common.Instances;
using Milki.OsuPlayer.Presentation;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages.Settings
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

        public AboutPage()
        {
            _mainWindow = WindowEx.GetCurrentFirst<MainWindow>();
            _configWindow = WindowEx.GetCurrentFirst<ConfigWindow>();
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
            CurrentVer.Content = Service.Get<UpdateInst>().CurrentVersionString;
            if (Service.Get<UpdateInst>().NewRelease != null)
                NewVersion.Visibility = Visibility.Visible;
            GetLastUpdate();
        }

        private void GetLastUpdate()
        {
            LastUpdate.Content = AppSettings.Default.LastUpdateCheck == null
                ? I18NUtil.GetString("ui-sets-content-never")
                : AppSettings.Default.LastUpdateCheck.Value.ToString(_dtFormat);
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            //todo: action
            CheckUpdate.IsEnabled = false;
            bool? hasNew;
            try
            {
                hasNew = await Service.Get<UpdateInst>().CheckUpdateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(_configWindow, I18NUtil.GetString("ui-sets-content-errorWhileCheckingUpdate") + Environment.NewLine +
                    (ex.InnerException?.Message ?? ex.Message),
                    _configWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CheckUpdate.IsEnabled = true;

            AppSettings.Default.LastUpdateCheck = DateTime.Now;
            GetLastUpdate();
            AppSettings.SaveDefault();
            if (hasNew == true)
            {
                NewVersion.Visibility = Visibility.Visible;
                NewVersion_Click(sender, e);
            }
            else
            {
                MessageBox.Show(_configWindow, I18NUtil.GetString("ui-sets-content-alreadyNewest"), _configWindow.Title,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NewVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_newVersionWindow != null && !_newVersionWindow.IsClosed)
                _newVersionWindow.Close();
            _newVersionWindow = new NewVersionWindow(Service.Get<UpdateInst>().NewRelease, _mainWindow);
            _newVersionWindow.ShowDialog();
        }

        private void LinkLicense_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Milkitic/Osu-Player/blob/master/LICENSE");
        }

        private void LinkPrivacy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This software will NOT collect any user information.");
        }
    }
}
