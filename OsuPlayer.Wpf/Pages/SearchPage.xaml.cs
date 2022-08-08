using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Common.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Presentation.Interaction;
using Milki.OsuPlayer.Presentation.ObjectModel;
using Milki.OsuPlayer.Shared.Dependency;
using Milki.OsuPlayer.UiComponents.PanelComponent;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
        private MainWindow _mainWindow;

        public SearchPageViewModel ViewModel { get; set; }

        private static readonly Binding _sourceBinding = new Binding(nameof(SearchPageViewModel.DataList))
        {
            Mode = BindingMode.OneWay
        };

        private static bool _minimal = false;
        public SearchPage()
        {
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            InitializeComponent();
        }

        public SearchPage Search(string keyword)
        {
            SearchBox.Text = keyword;
            return this;
        }

        private async void SearchPage_Initialized(object sender, EventArgs e)
        {
            ViewModel = (SearchPageViewModel)DataContext;
            await ViewModel.PlayListQueryAsync();
        }

        private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
        {
            var minimal = AppSettings.Default.Interface.MinimalMode;
            if (minimal != _minimal)
            {
                if (minimal)
                {
                    ResultCardList.ItemsSource = null;
                    ResultList.SetBinding(ItemsControl.ItemsSourceProperty, _sourceBinding);
                    ResultCardList.Visibility = Visibility.Collapsed;
                    ResultList.Visibility = Visibility.Visible;
                }
                else
                {
                    ResultList.ItemsSource = null;
                    ResultCardList.SetBinding(ItemsControl.ItemsSourceProperty, _sourceBinding);
                    ResultList.Visibility = Visibility.Collapsed;
                    ResultCardList.Visibility = Visibility.Visible;
                }

                _minimal = minimal;
                await ViewModel.PlayListQueryAsync();
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchText = ((TextBox)sender).Text;
            await ViewModel.PlayListQueryAsync();
        }

        private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;
        private void Panel_Loaded(object sender, RoutedEventArgs e)
        {
            _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
            ViewModel.GalleryWrapPanel = _virtualizingGalleryWrapPanel;
        }

        private void ResultListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelectedDefault();
        }

        private async void PlaySelectedDefault()
        {
            var map = await GetSelectedDefault();
            if (map == null)
                return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //    map.BeatmapFileName));
            await _controller.PlayNewAsync(map);
        }

        private async Task<Beatmap> GetSelectedDefault()
        {
            await using var appDbContext = new ApplicationDbContext();
            if (!(ResultList.SelectedItem is OrderedModel<Beatmap> beatmap)) return null;

            var allMaps = await appDbContext
                .GetBeatmapsFromFolder(beatmap.Model.FolderNameOrPath, beatmap.Model.InOwnDb);

            var map = allMaps.GetHighestDiff();
            return map;
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            List<Beatmap> beatmaps = ViewModel.DataList.Select(k => k.Model).ToList();
            if (beatmaps.Count <= 0) return;
            var group = beatmaps.GroupBy(k => k.FolderNameOrPath);
            List<Beatmap> newBeatmaps = group
                .Select(sb => sb.GetHighestDiff())
                .ToList();

            //if (map == null) return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //     map.BeatmapFileName));
            await _controller.PlayList.SetSongListAsync(newBeatmaps, true);
        }

        private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender, VirtualizingGalleryRoutedEventArgs e)
        {
            var beatmap = ViewModel.DataList[e.Index].Model;
            try
            {
                var fileName = await CommonUtils.GetThumbByBeatmapDbId(beatmap).ConfigureAwait(false);
                var thumbPath = Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg");
                if (beatmap.BeatmapThumb == null)
                {
                    Execute.OnUiThread(() =>
                    {
                        beatmap.BeatmapThumb = new BeatmapThumb
                        {
                            Beatmap = beatmap,
                            BeatmapId = beatmap.Id,
                            ThumbPath = thumbPath
                        };
                    });
                    await using var dbContext = new ApplicationDbContext();
                    dbContext.Add(beatmap.BeatmapThumb);
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    var update = beatmap.BeatmapThumb.ThumbPath != thumbPath;
                    Execute.OnUiThread(() => beatmap.BeatmapThumb.ThumbPath = thumbPath);
                    if (update)
                    {
                        await using var dbContext = new ApplicationDbContext();
                        dbContext.Thumbs.Update(beatmap.BeatmapThumb);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while loading panel item.");
            }

            Logger.Debug("VirtualizingGalleryWrapPanel: {0}", e.Index);
        }
    }
}
