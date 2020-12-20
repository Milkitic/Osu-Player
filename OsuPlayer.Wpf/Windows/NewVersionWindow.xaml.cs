using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Presentation;
using System.Windows;

namespace Milky.OsuPlayer.Windows
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

        private void Update_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var updateWindow = new UpdateWindow(_release, _mainWindow);
            updateWindow.Show();
            Close();
        }

        private void HtmlUrl_Click(object sender, RoutedEventArgs e)
        {
            ProcessLegacy.StartLegacy(_release.HtmlUrl);
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
