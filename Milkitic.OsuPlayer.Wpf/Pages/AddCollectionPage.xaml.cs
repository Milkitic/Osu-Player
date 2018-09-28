using Milkitic.OsuPlayer.Wpf.Data;
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

namespace Milkitic.OsuPlayer.Wpf.Pages
{
    /// <summary>
    /// AddCollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class AddCollectionPage : Page
    {
        private readonly MainWindow _window;
        private readonly SelectCollectionPage _page;

        public AddCollectionPage(MainWindow window, SelectCollectionPage page)
        {
            _window = window;
            _page = page;
            InitializeComponent();
        }
        public AddCollectionPage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            if (_page != null)
            {
                _page.FramePop.Navigate(null);
            }
            else
                _window.FramePop.Navigate(null);
            GC.SuppressFinalize(this);
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            DbOperator.AddCollection(CollectionName.Text);
            _window.UpdateCollections();
            Dispose();
            _page?.RefreshList();
        }
    }
}
