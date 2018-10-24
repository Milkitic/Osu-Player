using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Collection = Milkitic.OsuPlayer.Data.Collection;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private MainWindow ParentWindow { get; set; }
        private readonly Collection _collection;
        public List<BeatmapViewModel> ViewModels;
        private IEnumerable<BeatmapEntry> _entries;

        public CollectionPage(MainWindow mainWindow, Collection collection)
        {
            ParentWindow = mainWindow;
            _collection = collection;
            InitializeComponent();
            UpdateList();

            if (collection.Locked)
                BtnDelCol.Visibility = Visibility.Collapsed;
            //LblTitle.Content = _collection.Name;

            var item = ViewModels.FirstOrDefault(k =>
                k.GetIdentity().Equals(App.PlayerList.CurrentInfo.Identity));
            MapList.SelectedItem = item;
        }

        private void UpdateList()
        {
            CollectionInfo.DataContext = _collection;
            var infos = (List<MapInfo>)DbOperator.GetMapsFromCollection(_collection);
            _entries = App.Beatmaps.GetMapListFromDb(infos, false);
            ViewModels = _entries.Transform(true).ToList();
            for (var i = 0; i < ViewModels.Count; i++)
                ViewModels[i].Id = i.ToString("00");

            MapList.DataContext = ViewModels;
            ListCount.Content = ViewModels.Count();
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
            PlaySelected();
        }

        private void ItemPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapViewModel)MapList.SelectedItem;
            DbOperator.RemoveMapFromCollection(searchInfo.GetIdentity(), _collection);
            UpdateList();
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
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

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportPage.QueueEntries(_entries);
        }

        private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow, map.Creator));
        }

        private void ItemSearchSource_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow, map.SongSource));
        }

        private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaSelect.GetUnicode(map.Title, map.TitleUnicode)));
        }

        private void ItemExport_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ExportPage.QueueEntry(map);
        }

        private void ItemCollect_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.FramePop.Navigate(new SelectCollectionPage(ParentWindow, GetSelected()));
        }

        private void ItemSet_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapViewModel)MapList.SelectedItem;
            Process.Start($"https://osu.ppy.sh/b/{searchInfo.BeatmapId}");
        }

        private void ItemFolder_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapViewModel)MapList.SelectedItem;
            Process.Start(Path.Combine(Domain.OsuSongPath, searchInfo.FolderName));
        }

        private void PlaySelected()
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                map.BeatmapFileName));
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.None, PlayListMode.Collection, _entries);
        }

        private BeatmapEntry GetSelected()
        {
            if (MapList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapViewModel)MapList.SelectedItem;
            return _entries.GetBeatmapsetsByFolder(selectedItem.FolderName)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
        }
    }
}
