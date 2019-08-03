using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// ClosingPage.xaml 的交互逻辑
    /// </summary>
    public partial class ClosingPage : Page
    {
        private readonly MainWindow _mainWindow;

        public ClosingPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.FramePop.Navigate(null);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (AsDefault.IsChecked == true)
            {
                AppSettings.Current.General.ExitWhenClosed = RadioMinimum.IsChecked != true;
                AppSettings.SaveCurrent();
            }

            _mainWindow.FramePop.Navigate(null);
            if (RadioMinimum.IsChecked == true)
            {
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.ForceExit = true;
                _mainWindow.Close();
            }
        }
    }
}
