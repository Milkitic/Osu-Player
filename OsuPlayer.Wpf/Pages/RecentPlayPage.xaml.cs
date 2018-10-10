using Milkitic.OsuPlayer;
using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.Utils;
using osu_database_reader.Components.Beatmaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// RecentPlayPage.xaml 的交互逻辑
    /// </summary>
    public partial class RecentPlayPage : Page
    {
        private PageBox _pageBox;
        private IEnumerable<BeatmapEntry> _entries;
        public IEnumerable<BeatmapViewModel> ViewModels;
        public MainWindow ParentWindow { get; set; }

        public RecentPlayPage(MainWindow mainWindow)
        {
            ParentWindow = mainWindow;
            InitializeComponent();
            _pageBox = new PageBox(mainWindow.MainGrid, "_recent");
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
                k.GetIdentity().Equals(App.PlayerControl.NowIdentity));
            RecentList.SelectedItem = item;
        }

        private void LblCreator_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void Recent_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock)
                return;
            PlaySelected();
        }

        private void ItemPlay_Click(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void ItemNextPlay_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (RecentList.SelectedItem == null)
                return;
            var searchInfo = (BeatmapViewModel)RecentList.SelectedItem;
            DbOperator.RemoveFromRecent(searchInfo.GetIdentity());
            UpdateList();
            App.PlayerControl.RefreshPlayList(PlayerControl.FreshType.All, PlayListMode.RecentList);
        }

        private void BtnDelAll_Click(object sender, RoutedEventArgs e)
        {
            _pageBox.Show("提示", "真的要删除全部吗？", () =>
            {
                DbOperator.ClearRecent();
                UpdateList();
            });
        }

        private void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ItemCollect_Click(object sender, RoutedEventArgs e)
        {
            ParentWindow.FramePop.Navigate(new SelectCollectionPage(ParentWindow, GetSelected()));
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
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow, map.SongSource));
        }

        private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow, map.Creator));
        }

        private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaSelect.GetUnicode(map.Artist, map.ArtistUnicode)));
        }

        private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            ParentWindow.MainFrame.Navigate(new SearchPage(ParentWindow,
                MetaSelect.GetUnicode(map.Title, map.TitleUnicode)));
        }

        private void PlaySelected()
        {
            var map = GetSelected();
            if (map == null) return;

            ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                map.BeatmapFileName));
            App.PlayerControl.RefreshPlayList(PlayerControl.FreshType.None, PlayListMode.RecentList);
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
