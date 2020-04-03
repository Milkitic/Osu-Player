using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponent;
using Milky.OsuPlayer.UiComponent.FrontDialogComponent;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private readonly MainWindow _mainWindow;
        private IEnumerable<Beatmap> _entries;
        private readonly AppDbOperator _dbOperator = new AppDbOperator();
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

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
            var collectionInfo = _dbOperator.GetCollectionById(colId);
            ViewModel.CollectionInfo = collectionInfo;
            UpdateList();
        }

        public void UpdateList()
        {
            var infos = _dbOperator.GetMapsFromCollection(ViewModel.CollectionInfo);
            _entries = _dbOperator.GetBeatmapsByMapInfo(infos, TimeSortMode.AddTime);
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
                k.GetIdentity().Equals(_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
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
                _dbOperator.RemoveMapFromCollection(entry.GetIdentity(), ViewModel.CollectionInfo);
                if (!_controller.PlayList.CurrentInfo.Beatmap.GetIdentity().Equals(entry.GetIdentity()) ||
                    !ViewModel.CollectionInfo.LockedBool) continue;
                _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
                break;
            }
        }

        private void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, "确认删除收藏夹？", _mainWindow.Title, MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _dbOperator.RemoveCollection(ViewModel.CollectionInfo);
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
            await _controller.PlayNewAsync(map);
        }

        private Beatmap GetSelected()
        {
            if (MapList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapDataModel)MapList.SelectedItem;
            return _dbOperator.GetBeatmapsFromFolder(selectedItem.FolderName)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.CollectionInfo),
                DialogOptionFactory.EditCollectionOptions);
        }

        private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _dbOperator.GetBeatmapsFromFolder(dataModel.FolderName)
                .FirstOrDefault(k => k.Version == dataModel.Version);
        }

        private IEnumerable<Beatmap> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            var beatmaps = _entries.ToList();
            if (beatmaps.Count <= 0) return;

            await _controller.PlayList.SetSongListAsync(beatmaps, true);
        }

        private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
        {
            var dataModel = ViewModel.DisplayedBeatmaps[e.Index];
            try
            {
                var fileName = await CommonUtils.GetThumbByBeatmapDbId(dataModel).ConfigureAwait(false);
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
