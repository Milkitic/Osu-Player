using System;
using System.Windows;

namespace Milkitic.OsuPlayer.Windows
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
    }
}
