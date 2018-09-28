using Milkitic.OsuPlayer.Wpf.Data;
using Milkitic.OsuPlayer.Wpf.Models;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace Milkitic.OsuPlayer.Wpf.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private MainWindow ParentWindow { get; set; }
        private readonly Collection _collection;
        private List<BeatmapSearchInfo> _maps;

        public CollectionPage(MainWindow mainWindow, Collection collection)
        {
            ParentWindow = mainWindow;
            _collection = collection;
            InitializeComponent();
            UpdateList();
            LblTitle.Content = _collection.Name;
        }

        private void UpdateList()
        {
            var infos = (List<MapInfo>)DbOperator.GetMapsFromCollection(_collection);
            _maps = App.Beatmaps.GetMapListFromDb(infos).Transform(true).ToList();
            MapList.DataContext = _maps;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private void RecentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            if (e.OriginalSource is TextBlock)
                return;
            var searchInfo = (BeatmapSearchInfo)MapList.SelectedItem;
            var map = App.Beatmaps.GetBeatmapsetsByFolder(searchInfo.FolderName)
                .FirstOrDefault(k => k.Version == searchInfo.Version);
            if (map != null)
            {
                ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                    map.BeatmapFileName));
                ParentWindow.FillPlayList(false, false, PlayListMode.RecentList);
            }
            else
            {
                //todo
            }
        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapSearchInfo)MapList.SelectedItem;
            DbOperator.RemoveMapFromCollection(searchInfo.Version, searchInfo.FolderName, _collection);
            UpdateList();
            ParentWindow.FillPlayList(true, true, PlayListMode.RecentList);
        }

        private void LblCreator_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.PageBox.Show("提示", "确认删除收藏夹？", () =>
            {
                DbOperator.RemoveCollection(_collection);
                ParentWindow.MainFrame.Navigate(ParentWindow.Pages.RecentPlayPage);
                ParentWindow.UpdateCollections();
            });
        }
    }
}
