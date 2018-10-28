using System;
using System.Collections.Generic;
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

namespace Milkitic.OsuPlayer.Pages
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
                App.Config.General.ExitWhenClosed = RadioMinimum.IsChecked != true;
                App.SaveConfig();
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
