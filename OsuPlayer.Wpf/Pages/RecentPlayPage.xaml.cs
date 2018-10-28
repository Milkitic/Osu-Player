using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milkitic.OsuPlayer.Windows;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// RecentPlayPage.xaml 的交互逻辑
    /// </summary>
    public partial class RecentPlayPage : Page
    {
        private IEnumerable<BeatmapEntry> _entries;
        public IEnumerable<BeatmapViewModel> ViewModels;
        private readonly MainWindow _mainWindow;

        public RecentPlayPage(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
        }

        public void UpdateList()
        {
            _entries = App.Beatmaps.GetRecentListFromDb();
            ViewModels = _entries.Transform(true);
            RecentList.DataContext = ViewModels.ToList();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
            var item = ViewModels.FirstOrDefault(k =>
                k.GetIdentity().Equals(App.PlayerList.CurrentInfo.Identity));
            RecentList.SelectedItem = item;
        }

        private void Recent_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySelected();
        }

        private void ItemPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (RecentList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapViewModel)RecentList.SelectedItem;
            DbOperator.RemoveFromRecent(searchInfo.GetIdentity());
            UpdateList();
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.All, PlayListMode.RecentList);
        }

        private void BtnDelAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MsgBox.Show(_mainWindow, "真的要删除全部吗？", _mainWindow.Title, MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DbOperator.ClearRecent();
                UpdateList();
            }
        }

        private void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ItemCollect_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.FramePop.Navigate(new SelectCollectionPage(_mainWindow, GetSelected()));
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
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow, map.SongSource));
        }

        private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow, map.Creator));
        }

        private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow,
                MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.MainFrame.Navigate(new SearchPage(_mainWindow,
                MetaSelect.GetUnicode(map.Title, map.TitleUnicode)));
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

        private void PlaySelected()
        {
            var map = GetSelected();
            if (map == null) return;

            _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                map.BeatmapFileName));
            App.PlayerList.RefreshPlayList(PlayerList.FreshType.None, PlayListMode.RecentList);
        }

        private BeatmapEntry GetSelected()
        {
            if (RecentList.SelectedItem == null)
                return null;
            var selectedItem = (BeatmapViewModel)RecentList.SelectedItem;
            return _entries.GetBeatmapsetsByFolder(selectedItem.FolderName)
                .FirstOrDefault(k => k.Version == selectedItem.Version);
        }
    }
}
