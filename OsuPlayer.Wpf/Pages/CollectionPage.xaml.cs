using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.PanelComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// CollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class CollectionPage : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly MainWindow _mainWindow;
        private IEnumerable<Beatmap> _entries;
        private readonly SafeDbOperator _safeDbOperator = new SafeDbOperator();
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

        public async Task UpdateView(string colId)
        {
            await Task.Delay(1);

            var collectionInfo = _safeDbOperator.GetCollectionById(colId);
            if (collectionInfo == null) return;
            ViewModel.CollectionInfo = collectionInfo;
            UpdateList();
        }

        public void UpdateList()
        {
            var infos = _safeDbOperator.GetMapsFromCollection(ViewModel.CollectionInfo);
            _entries = _safeDbOperator.GetBeatmapsByMapInfo(infos, TimeSortMode.AddTime);
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
                if (!_safeDbOperator.TryRemoveMapFromCollection(entry.GetIdentity(), ViewModel.CollectionInfo))
                    continue;
                if (!_controller.PlayList.CurrentInfo.Beatmap.GetIdentity().Equals(entry.GetIdentity()) ||
                    !ViewModel.CollectionInfo.LockedBool) continue;
                _controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
                break;
            }
        }

        private void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveCollection"), _mainWindow.Title, MessageBoxButton.OKCancel,
                MessageBoxImage.Exclamation);
            if (result == MessageBoxResult.OK)
            {
                if (!_safeDbOperator.TryRemoveCollection(ViewModel.CollectionInfo)) return;
                _mainWindow.SwitchRecent.IsChecked = true;
                _mainWindow.UpdateCollections();
            }
        }

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportPage.QueueEntries(_entries);
        }

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
            return _safeDbOperator.GetBeatmapsFromFolder(selectedItem.FolderNameOrPath)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.CollectionInfo),
                DialogOptionFactory.EditCollectionOptions);
        }

        private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _safeDbOperator.GetBeatmapsFromFolder(dataModel.FolderNameOrPath)
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
                Logger.Error(ex, "Error while loading panel item.");
            }

            Logger.Debug("VirtualizingGalleryWrapPanel: {0}", e.Index);
        }

        private void Panel_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
