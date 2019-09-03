using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Models;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi.Collections;
using OSharp.Beatmap;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Control.Notification;
using Milky.OsuPlayer.Utils;
using BeatmapDbOperator = Milky.OsuPlayer.Common.Data.EF.BeatmapDbOperator;
using Collection = Milky.OsuPlayer.Common.Data.EF.Model.V1.Collection;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private readonly MainWindow _mainWindow;
        private IEnumerable<Beatmap> _entries;
        private BeatmapDbOperator _beatmapDbOperator = new BeatmapDbOperator();
        private AppDbOperator _appDbOperator = new AppDbOperator();
        public CollectionPageViewModel ViewModel { get; set; }
        public string Id { get; set; }

        public CollectionPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;

            ViewModel = (CollectionPageViewModel)this.DataContext;
        }
        public CollectionPage(string colId) : this()
        {
            var collectionInfo = _appDbOperator.GetCollectionById(colId);
            NavigateNewCollection(collectionInfo);
        }

        public CollectionPage NavigateNewCollection(Collection collectionInfo)
        {
            ViewModel.CollectionInfo = collectionInfo;
            var infos = _appDbOperator.GetMapsFromCollection(collectionInfo);
            _entries = _beatmapDbOperator.GetBeatmapsByMapInfo(infos, TimeSortMode.AddTime);
            ViewModel.Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(_entries.ToDataModelList(false));
            return this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //LblTitle.Content = _collection.Name;

            var item = ViewModel.Beatmaps?.FirstOrDefault(k =>
                k.GetIdentity().Equals(Services.Get<PlayerList>()?.CurrentInfo?.Identity));
            if (item != null)
                MapList.SelectedItem = item;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {

            }
            else
            {
                //var query = AppDbOperatorExt.FilterByKeyword(keyword);
                //UpdateView(query);
            }
        }

        private void UpdateView(IEnumerable<Beatmap> entries)
        {
            //ViewModel.Beatmaps = new ObservableCollection<BeatmapDataModel>(entries.ToDataModelList(false));
            //ListCount.Content = ViewModel.Beatmaps.Count;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            // todo
        }

        private void MapList_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void ItemPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private async void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var selected = MapList.SelectedItems;
            var entries = ConvertToEntries(selected.Cast<BeatmapDataModel>());
            foreach (var entry in entries)
            {
                _appDbOperator.RemoveMapFromCollection(entry.GetIdentity(), ViewModel.CollectionInfo);
            }
            //var dataModel = (BeatmapDataModel)MapList.SelectedItem;

            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
        }

        private void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, "确认删除收藏夹？", _mainWindow.Title, MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _appDbOperator.RemoveCollection(ViewModel.CollectionInfo);
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
            _mainWindow.MainFrame.Navigate(_mainWindow.Pages.SearchPage.Search(map.Creator));
        }

        private void ItemSearchSource_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(_mainWindow.Pages.SearchPage.Search(map.SongSource));
        }

        private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(_mainWindow.Pages.SearchPage.Search(
                MetaString.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(_mainWindow.Pages.SearchPage.Search(
                MetaString.GetUnicode(map.Title, map.TitleUnicode)));
        }

        private void ItemExport_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ExportPage.QueueEntry(map);
        }

        private void ItemCollect_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.FramePop.Navigate(new SelectCollectionPage(GetSelected()));
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
            await PlayController.Default.PlayNewFile(map);
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.None, PlayListMode.Collection, _entries);
        }

        private Beatmap GetSelected()
        {
            if (MapList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapDataModel)MapList.SelectedItem;
            return _beatmapDbOperator.GetBeatmapsFromFolder(selectedItem.FolderName)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.FramePop.Navigate(new EditCollectionPage(_mainWindow, ViewModel.CollectionInfo));
        }

        private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _beatmapDbOperator.GetBeatmapsFromFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<Beatmap> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private void Page_Initialized(object sender, System.EventArgs e)
        {
            var helper = new GridViewHelper(MapList);
            helper.OnMouseDoubleClick(MapList_MouseDoubleClick);
        }
    }
}
