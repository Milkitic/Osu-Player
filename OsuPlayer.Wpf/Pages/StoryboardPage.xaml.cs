using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// StoryboardPage.xaml 的交互逻辑
    /// </summary>
    public partial class StoryboardPage : Page
    {
        private readonly MainWindow _mainWindow;

        public StoryboardPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //if(StoryboardVm.Default.BeatmapModels)
            if (AppSettings.Default.General.SbScanned)
            {
                ScanScene.Visibility = Visibility.Collapsed;
                ScanScene.IsEnabled = false;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // todo
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            StoryboardVm.Default.IsScanning = true;

            await StoryboardVm.Default.ScanBeatmap().ConfigureAwait(false);

            StoryboardVm.Default.IsScanning = false;
            AppSettings.Default.General.SbScanned = true;
            AppSettings.SaveDefault();
        }
    }
}
