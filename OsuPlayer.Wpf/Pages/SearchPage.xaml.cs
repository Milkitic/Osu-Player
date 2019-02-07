using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        private readonly Stopwatch _querySw = new Stopwatch();
        private bool _queryLock;
        public MainWindow ParentWindow { get; set; }

        public SearchPage(MainWindow mainWindow)
        {
            ParentWindow = mainWindow;
            InitializeComponent();
        }

        public SearchPage(MainWindow mainWindow, string searchKey) : this(mainWindow)
        {
            SearchBox.Text = searchKey;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await PlayListQueryAsync();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await PlayListQueryAsync();
        }

        private async Task PlayListQueryAsync()
        {
            if (App.Beatmaps == null)
                return;

            //SortEnum sortEnum = (SortEnum)cbSortType.SelectedItem;
            SortMode sortMode = SortMode.Artist;
            _querySw.Restart();
            if (_queryLock)
                return;
            _queryLock = true;
            await Task.Run(() =>
            {
                while (_querySw.ElapsedMilliseconds < 300)
                    Thread.Sleep(1);
                _querySw.Stop();
                _queryLock = false;
                string keyword = null;
                Dispatcher.Invoke(() => { keyword = SearchBox.Text.Trim(); });

                var sorted = string.IsNullOrEmpty(keyword)
                    ? App.Beatmaps.SortBy(sortMode).Transform(false)
                    : EntryQuery.GetListByKeyword(keyword, App.Beatmaps).SortBy(sortMode).Transform(false);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResultList.DataContext = sorted;
                }));
            });
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
            var page = new DiffSelectPage(ParentWindow,
                App.Beatmaps.GetBeatmapsetsByFolder(ok.GetIdentity().FolderName));
            page.Callback = async () =>
            {
                await ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, page.SelectedMap.FolderName,
                      page.SelectedMap.BeatmapFileName));
                App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, PlayListMode.RecentList);
                ParentWindow.FramePop.Navigate(null);
            };
            ParentWindow.FramePop.Navigate(page);
        }

        private void ItemSearchMapper_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow, map.Creator));
        }

        private void ItemSearchSource_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow, map.SongSource));
        }

        private void ItemSearchArtist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaSelect.GetUnicode(map.Title, map.TitleUnicode)));
        }

        private void ItemExport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null) return;
            ExportPage.QueueEntry(map);
        }

        private void ItemCollect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ResultList.SelectedItem == null)
                return;
            var ok = (BeatmapDataModel)ResultList.SelectedItem;
            var page = new DiffSelectPage(ParentWindow,
                App.Beatmaps.GetBeatmapsetsByFolder(ok.GetIdentity().FolderName));
            page.Callback = () =>
            {
                ParentWindow.FramePop.Navigate(new SelectCollectionPage(ParentWindow,
                    App.Beatmaps.GetBeatmapsetsByFolder(page.SelectedMap.FolderName)
                        .FirstOrDefault(k => k.Version == page.SelectedMap.Version)));
            };
            ParentWindow.FramePop.Navigate(page);
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
            if (map == null) return;
            await ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                map.BeatmapFileName));
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, PlayListMode.RecentList);
        }

        private BeatmapEntry GetSelectedDefault()
        {
            if (ResultList.SelectedItem == null)
                return null;
            var map = App.Beatmaps.GetBeatmapsetsByFolder(((BeatmapDataModel)ResultList.SelectedItem).FolderName)
                .GetHighestDiff();
            return map;
        }

    }
}

