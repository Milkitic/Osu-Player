using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// FindPage.xaml 的交互逻辑
    /// </summary>
    public partial class FindPage : Page
    {
        private readonly MainWindow _mainWindow;

        public FindPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow; 
        }
    }
}
