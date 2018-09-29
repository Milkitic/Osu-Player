using System.Windows.Controls;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// FindPage.xaml 的交互逻辑
    /// </summary>
    public partial class FindPage : Page
    {
        private readonly MainWindow _mainWindow;

        public FindPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }
    }
}
