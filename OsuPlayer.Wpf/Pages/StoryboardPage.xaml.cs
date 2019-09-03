using System.Windows;
using System.Windows.Controls;
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
            // todo
        }
        
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // todo
        }
    }
}
