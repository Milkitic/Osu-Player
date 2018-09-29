using System;
using System.Windows;
using System.Windows.Controls;
using Milkitic.OsuPlayer.Data;

namespace Milkitic.OsuPlayer.Pages
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
