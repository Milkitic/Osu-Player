using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.PanelComponent;
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
using System.Windows.Input;
using Milky.OsuPlayer.Data;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();

        private MainWindow _mainWindow;

        public SearchPageViewModel ViewModel { get; set; }

        private static Binding _sourceBinding = new Binding(nameof(SearchPageViewModel.DisplayedMaps))
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
            var map = GetSelectedDefault();
            if (map == null)
                return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //    map.BeatmapFileName));
            await _controller.PlayNewAsync(map);
        }

        private Beatmap GetSelectedDefault()
        {
            if (ResultList.SelectedItem == null)
                return null;
            var map = SafeDbOperator
                .GetBeatmapsFromFolder(((BeatmapDataModel)ResultList.SelectedItem).FolderNameOrPath)
                .GetHighestDiff();
            return map;
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            List<Beatmap> beatmaps = ViewModel.SearchedDbMaps;
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
            var dataModel = ViewModel.DisplayedMaps[e.Index];
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
    }
}
