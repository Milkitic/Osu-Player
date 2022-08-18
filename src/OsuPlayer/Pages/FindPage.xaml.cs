using System.Windows;
using System.Windows.Controls;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages
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
