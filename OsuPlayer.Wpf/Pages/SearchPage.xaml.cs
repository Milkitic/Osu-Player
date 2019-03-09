using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Models;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap;
using osu_database_reader.Components.Beatmaps;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        public MainWindow ParentWindow { get; set; }

        public SearchPageViewModel ViewModel { get; set; }

        public SearchPage(MainWindow mainWindow)
        {
            ParentWindow = mainWindow;
            InitializeComponent();
            ViewModel = (SearchPageViewModel)DataContext;
        }

        public SearchPage(MainWindow mainWindow, string searchKey) : this(mainWindow)
        {
            SearchBox.Text = searchKey;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.PlayListQueryAsync();
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
            var page = new DiffSelectPage(ParentWindow,
                InstanceManage.GetInstance<OsuDbInst>().Beatmaps.FilterByFolder(ok.GetIdentity().FolderName));
            page.Callback = async () =>
            {
                await ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, page.SelectedMap.FolderName,
                      page.SelectedMap.BeatmapFileName));
                InstanceManage.GetInstance<PlayerList>().RefreshPlayList(PlayerList.FreshType.All, PlayListMode.RecentList);
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
                MetaString.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var map = GetSelectedDefault();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaString.GetUnicode(map.Title, map.TitleUnicode)));
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
                InstanceManage.GetInstance<OsuDbInst>().Beatmaps.FilterByFolder(ok.GetIdentity().FolderName));
            page.Callback = () =>
            {
                ParentWindow.FramePop.Navigate(new SelectCollectionPage(ParentWindow,
                    InstanceManage.GetInstance<OsuDbInst>().Beatmaps.FilterByFolder(page.SelectedMap.FolderName)
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
            InstanceManage.GetInstance<PlayerList>().RefreshPlayList(PlayerList.FreshType.All, PlayListMode.RecentList);
        }

        private BeatmapEntry GetSelectedDefault()
        {
            if (ResultList.SelectedItem == null)
                return null;
            var map = InstanceManage.GetInstance<OsuDbInst>().Beatmaps.FilterByFolder(((BeatmapDataModel)ResultList.SelectedItem).FolderName)
                .GetHighestDiff();
            return map;
        }

    }
}

