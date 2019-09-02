using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Instances;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Common.Player;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi.Collections;
using OSharp.Beatmap;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Utils;
using BeatmapDbOperator = Milky.OsuPlayer.Common.Data.EF.BeatmapDbOperator;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// RecentPlayPage.xaml 的交互逻辑
    /// </summary>
    public partial class RecentPlayPage : Page
    {
        private ObservableCollection<Beatmap> _recentBeatmaps;
        public NumberableObservableCollection<BeatmapDataModel> DataModels;
        private readonly MainWindow _mainWindow;
        private BeatmapDbOperator _beatmapOperator = new BeatmapDbOperator();
        private AppDbOperator _appDbOperator = new AppDbOperator();

        public RecentPlayPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            InitializeComponent();
        }

        public void UpdateList()
        {
            _recentBeatmaps = new ObservableCollection<Beatmap>(
                _beatmapOperator.GetBeatmapsByMapInfo(_appDbOperator.GetRecentList(), TimeSortMode.PlayTime));
            DataModels = new NumberableObservableCollection<BeatmapDataModel>(_recentBeatmaps.ToDataModelList(false));
            RecentList.DataContext = DataModels.ToList();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
            var item = DataModels.FirstOrDefault(k =>
                k.GetIdentity().Equals(Services.Get<PlayerList>().CurrentInfo?.Identity));
            RecentList.SelectedItem = item;
        }

        private void RecentList_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void ItemPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private async void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (RecentList.SelectedItem == null)
                return;
            var selected = RecentList.SelectedItems;
            var entries = ConvertToEntries(selected.Cast<BeatmapDataModel>());
            //var searchInfo = (BeatmapDataModel)RecentList.SelectedItem;
            foreach (var entry in entries)
            {
                _appDbOperator.RemoveFromRecent(entry.GetIdentity());
            }
            UpdateList();
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
        }

        private void BtnDelAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, "真的要删除全部吗？", _mainWindow.Title, MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _appDbOperator.ClearRecent();
                UpdateList();
            }
        }

        private void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ItemCollect_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.FramePop.Navigate(new SelectCollectionPage(GetSelected()));
        }

        private void ItemExport_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ExportPage.QueueEntry(map);
        }

        private void ItemSearchSource_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(_mainWindow.Pages.SearchPage.Search(map.SongSource));
        }

        private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(_mainWindow.Pages.SearchPage.Search(map.Creator));
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

        private void ItemSet_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            Process.Start($"https://osu.ppy.sh/b/{map.BeatmapId}");
        }

        private void ItemFolder_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            Process.Start(Path.Combine(Domain.OsuSongPath, map.FolderName));
        }

        private async void PlaySelected()
        {
            var map = GetSelected();
            if (map == null) return;

            //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
            //       map.BeatmapFileName));
            await PlayController.Default.PlayNewFile(map);
            await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.None, PlayListMode.RecentList);
        }

        private Beatmap GetSelected()
        {
            if (RecentList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapDataModel)RecentList.SelectedItem;

            return _recentBeatmaps.FirstOrDefault(k =>
                k.FolderName == selectedItem.FolderName &&
                k.Version == selectedItem.Version
            );
        }

        private Beatmap ConvertToEntry(BeatmapDataModel dataModel)
        {
            return _recentBeatmaps.FirstOrDefault(k =>
                k.FolderName == dataModel.FolderName &&
                k.Version == dataModel.Version
            );
        }

        private IEnumerable<Beatmap> ConvertToEntries(IEnumerable<BeatmapDataModel> dataModels)
        {
            return dataModels.Select(ConvertToEntry);
        }

        private void Page_Initialized(object sender, System.EventArgs e)
        {
            var helper = new GridViewHelper(RecentList);
            helper.OnMouseDoubleClick(RecentList_MouseDoubleClick);
        }
    }
}
