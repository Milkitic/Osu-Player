using System.Diagnostics;
using System.Windows;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Presentation;

namespace Milki.OsuPlayer.Windows
{
    /// <summary>
    /// NewVersionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewVersionWindow : WindowEx
    {
        private readonly GithubRelease _release;
        private readonly MainWindow _mainWindow;

        public NewVersionWindow(GithubRelease release, MainWindow mainWindow)
        {
            _release = release;
            _mainWindow = mainWindow;
            InitializeComponent();
            MainGrid.DataContext = _release;
        }

        //private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        //{
        //    var p = e.Parameter.ToString();
        //    if (p == "later")
        //    {
        //        Close();
        //    }
        //    else if (p == "ignore")
        //    {
        //        AppSettings.Default.IgnoredVer = _release.NewVerString;
        //        AppSettings.SaveDefault();
        //        Close();
        //    }
        //    else if (p == "update")
        //    {
        //        UpdateWindow updateWindow = new UpdateWindow(_release, _mainWindow);
        //        updateWindow.Show();
        //        Close();
        //    }
        //    else
        //        Process.Start(p);
        //}

        private void Update_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var updateWindow = new UpdateWindow(_release, _mainWindow);
            updateWindow.Show();
            Close();
        }

        private void HtmlUrl_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_release.HtmlUrl);
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.IgnoredVer = _release.NewVerString;
            AppSettings.SaveDefault();
            Close();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
