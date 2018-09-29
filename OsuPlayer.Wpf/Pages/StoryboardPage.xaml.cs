using System;
using System.Windows;
using System.Windows.Controls;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// StoryboardPage.xaml 的交互逻辑
    /// </summary>
    public partial class StoryboardPage : Page
    {
        private readonly MainWindow _mainWindow;
        private static StoryboardWindow _sbWindow;

        public StoryboardPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_sbWindow == null)
            {
                _sbWindow = new StoryboardWindow();
                _mainWindow.Deactivated += MainWindow_Deactivated;
                _mainWindow.Activated += MainWindow_Activated;
                _mainWindow.Closing += MainWindow_Closing;
                _mainWindow.LocationChanged += _mainWindow_LocationChanged;
            }
            _sbWindow.Show();
            ReLocate();
        }

        private void _mainWindow_LocationChanged(object sender, EventArgs e)
        {
            ReLocate();
        }

        private void ReLocate()
        {
            Window window = Window.GetWindow(SbScene);
            if (window == null) return;
            Point point = SbScene.TransformToAncestor(window).Transform(new Point(0, 0));
            _sbWindow.Left = window.Left + point.X + 8;
            _sbWindow.Top = window.Top + point.Y + 31;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _sbWindow.Close();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            _sbWindow.Topmost = true;
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            _sbWindow.Topmost = false;
        }
    }
}
