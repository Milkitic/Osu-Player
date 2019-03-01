using Milky.OsuPlayer.Models.Github;
using Milky.WpfApi;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// NewVersionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewVersionWindow : WindowBase
    {
        private readonly Release _release;
        private readonly MainWindow _mainWindow;

        public NewVersionWindow(Release release, MainWindow mainWindow)
        {
            _release = release;
            _mainWindow = mainWindow;
            InitializeComponent();
            MainGrid.DataContext = _release;
        }

        private void WindowBase_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void OpenHyperlink(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var p = e.Parameter.ToString();
            if (p == "later")
            {
                Close();
            }
            else if (p == "ignore")
            {
                App.Config.IgnoredVer = _release.NewVerString;
                App.SaveConfig();
                Close();
            }
            else if (p == "update")
            {
                UpdateWindow updateWindow = new UpdateWindow(_release, _mainWindow);
                updateWindow.Show();
                Close();
            }
            else
                Process.Start(p);
        }
    }
}
