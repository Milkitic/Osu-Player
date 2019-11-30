using System;
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
using System.Windows.Data;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Control.Notification;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;

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

        private static Binding _sourceBinding = new Binding(nameof(CollectionPageViewModel.DisplayedBeatmaps))
        {
            Mode = BindingMode.OneWay
        };

        private bool _minimal = false;

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
            UpdateView(colId);
        }

        public void UpdateView(string colId)
        {
            var collectionInfo = _appDbOperator.GetCollectionById(colId);
            ViewModel.CollectionInfo = collectionInfo;
            UpdateList();
        }

        public void UpdateList()
        {
            var infos = _appDbOperator.GetMapsFromCollection(ViewModel.CollectionInfo);
            _entries = _beatmapDbOperator.GetBeatmapsByMapInfo(infos, TimeSortMode.AddTime);
            ViewModel.Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(_entries.ToDataModelList(false));
            ViewModel.DisplayedBeatmaps = ViewModel.Beatmaps;
            ListCount.Content = ViewModel.Beatmaps.Count;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var minimal = AppSettings.Default.Interface.MinimalMode;
            if (minimal != _minimal)
            {
                if (minimal)
                {
                    MapCardList.ItemsSource = null;
                    MapList.SetBinding(ItemsControl.ItemsSourceProperty, _sourceBinding);
                    MapCardList.Visibility = Visibility.Collapsed;
                    MapList.Visibility = Visibility.Visible;
                }
                else
                {
                    MapList.ItemsSource = null;
                    MapCardList.SetBinding(ItemsControl.ItemsSourceProperty, _sourceBinding);
                    MapList.Visibility = Visibility.Collapsed;
                    MapCardList.Visibility = Visibility.Visible;
                }

                _minimal = minimal;
            }

            var item = ViewModel.Beatmaps?.FirstOrDefault(k =>
                k.GetIdentity().Equals(Services.Get<PlayerList>()?.CurrentInfo?.Identity));
            if (item != null)
                MapList.SelectedItem = item;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyword = SearchBox.Text.Trim();
            ViewModel.DisplayedBeatmaps = string.IsNullOrWhiteSpace(keyword)
                ? ViewModel.Beatmaps
                : new NumberableObservableCollection<BeatmapDataModel>(ViewModel.Beatmaps.GetByKeyword(keyword));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            // todo
        }

        private void MapListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
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
                _mainWindow.SwitchRecent.IsChecked = true;
                _mainWindow.UpdateCollections();
            }
        }

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportPage.QueueEntries(_entries);
        }

        //private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelected();
        //    if (map == null) return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.Creator));
        //}

        //private void ItemSearchSource_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelected();
        //    if (map == null) return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.SongSource));
        //}

        //private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelected();
        //    if (map == null) return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoArtist));
        //}

        //private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelected();
        //    if (map == null) return;
        //    _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoTitle));
        //}

        //private void ItemExport_Click(object sender, RoutedEventArgs e)
        //{
        //    var map = GetSelected();
        //    if (map == null) return;
        //    ExportPage.QueueEntry(map);
        //}

        //private void ItemCollect_Click(object sender, RoutedEventArgs e)
        //{
        //    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(GetSelected()),
        //        DialogOptionFactory.SelectCollectionOptions);
        //}

        //private void ItemSet_Click(object sender, RoutedEventArgs e)
        //{
        //    if (MapList.SelectedItem == null)
        //        return;
        //    var searchInfo = (BeatmapDataModel)MapList.SelectedItem;
        //    Process.Start($"https://osu.ppy.sh/b/{searchInfo.BeatmapId}");
        //}

        //private void ItemFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    if (MapList.SelectedItem == null)
        //        return;
        //    var searchInfo = (BeatmapDataModel)MapList.SelectedItem;
        //    var dir = searchInfo.InOwnDb
        //        ? Path.Combine(Domain.CustomSongPath, searchInfo.FolderName)
        //        : Path.Combine(Domain.OsuSongPath, searchInfo.FolderName);
        //    if (!Directory.Exists(dir))
        //    {
        //        Notification.Show(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
        //        return;
        //    }

        //    Process.Start(dir);
        //}

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
            FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.CollectionInfo),
                DialogOptionFactory.EditCollectionOptions);
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

        private void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
        {
            var dataModel = ViewModel.DisplayedBeatmaps[e.Index];
            try
            {
                var fileName = await Util.GetThumbByBeatmapDbId(dataModel).ConfigureAwait(false);
                Execute.OnUiThread(() => dataModel.ThumbPath = Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine(e.Index);
        }

        private void Panel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
