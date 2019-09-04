using System;
using System.Collections.Generic;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Control;
using BeatmapDbOperator = Milky.OsuPlayer.Common.Data.EF.BeatmapDbOperator;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private BeatmapDbOperator _beatmapDbOperator;
        private MainWindow _mainWindow;

        public SearchPageViewModel ViewModel { get; set; }

        public SearchPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow; ;
            _beatmapDbOperator = new BeatmapDbOperator();

            ViewModel = (SearchPageViewModel)DataContext;
            SearchBox.Text = "";
        }

        //private async void Page_Initialized(object sender, EventArgs e)
        //{
        //    await ViewModel.PlayListQueryAsync();
        //}

        public SearchPage Search(string keyword)
        {
            SearchBox.Text = keyword;
            return this;
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchText = ((TextBox)sender).Text;
            await ViewModel.PlayListQueryAsync();
        }

        private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelectedDefault();
        }

        private void ItemPlay_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ResultList.SelectedItem == null)
                return;
            var ok = (BeatmapDataModel)ResultList.SelectedItem;
            var page = new DiffSelectPage(_mainWindow,
                _beatmapDbOperator.GetBeatmapsFromFolder(ok.GetIdentity().FolderName));
            page.Callback = async () =>
            {
                //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, page.SelectedMap.FolderName,
                //      page.SelectedMap.BeatmapFileName));
                var map = _beatmapDbOperator.GetBeatmapByIdentifiable(page.SelectedMap);
                await PlayController.Default.PlayNewFile(map);
                await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
                _mainWindow.FramePop.Navigate(null);
            };
            _mainWindow.FramePop.Navigate(page);
        }

        private void ItemSearchMapper_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.Creator));
        }

        private void ItemSearchSource_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.SongSource));
        }

        private void ItemSearchArtist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoArtist));
        }

        private void ItemSearchTitle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoTitle));
        }

        private void ItemExport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            ExportPage.QueueEntry(map);
        }

        private void ItemCollect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ResultList.SelectedItem == null)
                return;
            var ok = (BeatmapDataModel)ResultList.SelectedItem;
            var page = new DiffSelectPage(_mainWindow,
                _beatmapDbOperator.GetBeatmapsFromFolder(ok.GetIdentity().FolderName));
            page.Callback = () =>
            {
                _mainWindow.FramePop.Navigate(new SelectCollectionPage(
                    _beatmapDbOperator.GetBeatmapsFromFolder(page.SelectedMap.FolderName)
                        .FirstOrDefault(k => k.Version == page.SelectedMap.Version))
                );
            };
            _mainWindow.FramePop.Navigate(page);
        }

        private void ItemSet_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
        }

        private void ItemFolder_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            Process.Start(Path.Combine(Domain.OsuSongPath, map.FolderName));
        }

        private async void PlaySelectedDefault()
        {
            var map = GetSelectedDefault();
            if (map == null)
                return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //    map.BeatmapFileName));
            await PlayController.Default.PlayNewFile(map);
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
        }

        private Beatmap GetSelectedDefault()
        {
            if (ResultList.SelectedItem == null)
                return null;
            var map = _beatmapDbOperator.GetBeatmapsFromFolder(((BeatmapDataModel)ResultList.SelectedItem).FolderName)
                .GetHighestDiff();
            return map;
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            List<Beatmap> beatmaps = ViewModel.SearchedDbMaps;
            if (beatmaps.Count <= 0) return;
            var group = beatmaps.GroupBy(k => k.FolderName);
            List<Beatmap> newBeatmaps = group
                .Select(sb => sb.GetHighestDiff())
                .ToList();

            //if (map == null) return;
            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //     map.BeatmapFileName));
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.None, PlayListMode.Collection, newBeatmaps);
            await PlayController.Default.PlayNewFile(newBeatmaps[0]);

        }

        private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
