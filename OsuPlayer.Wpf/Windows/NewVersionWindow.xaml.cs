using Milky.WpfApi;
using System;
using System.IO;
using System.Windows;
using Milky.OsuPlayer.Models.Github;

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

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindow updateWindow = new UpdateWindow(_release, _mainWindow);
            updateWindow.Show();
            Close();
        }

        private void BtnIgnore_Click(object sender, RoutedEventArgs e)
        {
            App.Config.IgnoredVer = _release.NewVerString;
            App.SaveConfig();
            Close();
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WindowBase_Loaded(object sender, RoutedEventArgs e)
        {
         
        }
    }
}
