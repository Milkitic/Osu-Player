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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Milky.OsuPlayer.Shared.Models;
using Beatmap = Milky.OsuPlayer.Data.Models.Beatmap;

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

        public CollectionPage(Guid colId) : this()
        {
            UpdateView(colId);
        }

        public async Task UpdateView(Guid colId)
        {
            await Task.Delay(1);
            await using var dbContext = new ApplicationDbContext();
            var collectionInfo = await dbContext.GetCollection(colId);
            if (collectionInfo == null) return;
            ViewModel.CollectionInfo = collectionInfo;
            await UpdateList();
        }

        public async Task UpdateList()
        {
            await using var dbContext = new ApplicationDbContext();
            _entries = (await dbContext.GetBeatmapsFromCollection(ViewModel.CollectionInfo, 0, int.MaxValue))
                .Collection; // todo: pagination
            ViewModel.Beatmaps = new ObservableCollection<Beatmap>(_entries);
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
                : new ObservableCollection<Beatmap>(ViewModel.Beatmaps); // todo: search
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            // todo
        }

        private async void MapListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            await PlaySelected();
        }

        private async void ItemPlay_Click(object sender, RoutedEventArgs e)
        {
            await PlaySelected();
        }

        private async void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MapList.SelectedItem == null)
                return;
            var selected = MapList.SelectedItems;
            var beatmaps = selected.Cast<Beatmap>().ToList();

            await using var dbContext = new ApplicationDbContext();
            await dbContext.DeleteBeatmapsFromCollection(beatmaps, ViewModel.CollectionInfo);
            var currentInfo = _controller.PlayList.CurrentInfo;
            if (ViewModel.CollectionInfo.IsLocked &&
                beatmaps.Any(k => k.Id == currentInfo.Beatmap.Id))
            {
                currentInfo.BeatmapDetail.Metadata.IsFavorite = false;
            }

        }

        private async void BtnDelCol_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveCollection"), _mainWindow.Title, MessageBoxButton.OKCancel,
                MessageBoxImage.Exclamation);
            if (result != MessageBoxResult.OK) return;
            await using var dbContext = new ApplicationDbContext();

            await dbContext.DeleteCollection(ViewModel.CollectionInfo);
            _mainWindow.SwitchRecent.IsChecked = true;
            await _mainWindow.UpdateCollections();
        }

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportPage.QueueEntries(_entries);
        }

        private async Task PlaySelected()
        {
            var map = await GetSelected();
            if (map == null) return;
            await _controller.PlayNewAsync(map);
        }

        private async Task<Beatmap> GetSelected()
        {
            if (MapList.SelectedItem == null)
                return null;
            var selectedItem = (Beatmap)MapList.SelectedItem;
            await using var dbContext = new ApplicationDbContext();
            var beatmaps = await dbContext.GetBeatmapsFromFolder(selectedItem.FolderNameOrPath, selectedItem.InOwnDb);
            return beatmaps.FirstOrDefault(k => k.Version == selectedItem.Version);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.CollectionInfo),
                DialogOptionFactory.EditCollectionOptions);
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
                Execute.OnUiThread(() => dataModel.BeatmapThumb.ThumbPath =
                    Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg")
                );
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
