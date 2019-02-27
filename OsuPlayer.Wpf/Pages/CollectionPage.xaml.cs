using Milky.OsuPlayer;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi.Collections;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Data.EF.Model;
using Milky.OsuPlayer.Models;
using Collection = Milky.OsuPlayer.Data.EF.Model.Collection;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private readonly MainWindow _mainWindow;
        private IEnumerable<BeatmapEntry> _entries;
        public CollectionPageViewModel ViewModel { get; set; }
        public string Id { get; set; }

        public CollectionPage(MainWindow mainWindow, Collection collectionInfo)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            ViewModel = (CollectionPageViewModel)this.DataContext;
            ViewModel.CollectionInfo = collectionInfo;
            var infos = (List<MapInfo>)DbOperate.GetMapsFromCollection(collectionInfo);
            _entries = App.Beatmaps.GetMapListFromDb(infos, false);
            ViewModel.Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(_entries.ToViewModel(true));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //LblTitle.Content = _collection.Name;

            var item = ViewModel.Beatmaps?.FirstOrDefault(k =>
                k.GetIdentity().Equals(App.PlayerList?.CurrentInfo?.Identity));
            if (item != null)
                MapList.SelectedItem = item;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
                UpdateList();
            else
            {
                var query = BeatmapEntryQuery.ByKeyword(_entries, keyword);
                UpdateView(query);
            }

        }

        private void UpdateList()
        {
            //CollectionInfoGrid.DataContext = _collection;
            //var infos = (List<MapInfo>)DbOperator.GetMapsFromCollection(_collection);
            //_entries = App.Beatmaps.GetMapListFromDb(infos, false);
            //UpdateView(_entries);
        }

        private void UpdateView(IEnumerable<BeatmapEntry> entries)
        {
            //ViewModel.Beatmaps = new ObservableCollection<BeatmapDataModel>(entries.ToViewModel(true));
            //ListCount.Content = ViewModel.Beatmaps.Count;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {

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
            var selected = MapList.SelectedItems;
            var entries = ConvertToEntries(selected.Cast<BeatmapDataModel>());
            foreach (var entry in entries)
            {
                DbOperate.RemoveMapFromCollection(entry.GetIdentity(), ViewModel.CollectionInfo);
            }
            //var dataModel = (BeatmapDataModel)MapList.SelectedItem;
            UpdateList();
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
        }

        private void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            var result = MsgBox.Show(_mainWindow, "确认删除收藏夹？", _mainWindow.Title, MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DbOperate.RemoveCollection(ViewModel.CollectionInfo);
                _mainWindow.MainFrame.Navigate(_mainWindow.Pages.RecentPlayPage);
                _mainWindow.UpdateCollections();
            }
        }

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportPage.QueueEntries(_entries);
        }

        private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow, map.Creator));
        }

        private void ItemSearchSource_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow, map.SongSource));
        }

        private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow,
                MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow,
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
            _mainWindow.FramePop.Navigate(new SelectCollectionPage(_mainWindow, GetSelected()));
        }

        private void ItemSet_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapDataModel)MapList.SelectedItem;
            Process.Start($"https://osu.ppy.sh/b/{searchInfo.BeatmapId}");
        }

        private void ItemFolder_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapDataModel)MapList.SelectedItem;
            Process.Start(Path.Combine(Domain.OsuSongPath, searchInfo.FolderName));
        }

        private async void PlaySelected()
        {
            var map = GetSelected();
            if (map == null) return;
            await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                 map.BeatmapFileName));
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.None, PlayListMode.Collection, _entries);
        }

        private BeatmapEntry GetSelected()
        {
            if (MapList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapDataModel)MapList.SelectedItem;
            return _entries.ByFolder(selectedItem.FolderName)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.FramePop.Navigate(new EditCollectionPage(_mainWindow, ViewModel.CollectionInfo));
        }

        private BeatmapEntry ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _entries.ByFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<BeatmapEntry> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }
    }
}
