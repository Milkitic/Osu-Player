using Milkitic.OsuPlayer.Wpf.Data;
using osu_database_reader.Components.Beatmaps;
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
using Collection = Milkitic.OsuPlayer.Wpf.Data.Collection;

namespace Milkitic.OsuPlayer.Wpf.Pages
{
    /// <summary>
    /// AddCollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionPage : Page
    {
        private readonly MainWindow _mainWindow;
        private readonly BeatmapEntry _entry;

        public SelectCollectionPage(MainWindow mainWindow, BeatmapEntry entry)
        {
            _mainWindow = mainWindow;
            _entry = entry;
            InitializeComponent();
            RefreshList();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            _mainWindow.FramePop.Navigate(null);
            GC.SuppressFinalize(this);
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(_mainWindow, this));
        }

        public void RefreshList()
        {
            var list = (List<Collection>)DbOperator.GetCollections();
            list.Reverse();
            CollectionList.DataContext = list;
        }

        private void CollectionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CollectionList.SelectedItem == null)
                return;
            Collection col = (Collection)CollectionList.SelectedItem;
            DbOperator.AddMapToCollection(_entry, col);
            Dispose();
        }
    }
}
