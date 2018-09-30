using Milkitic.OsuPlayer.Control;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer;
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
        public MainWindow ParentWindow { get; set; }

        public RecentPlayPage(MainWindow mainWindow)
        {
            ParentWindow = mainWindow;
            InitializeComponent();
            _pageBox = new PageBox(mainWindow.MainGrid, "_recent");
        }

        public void UpdateList()
        {
            RecentList.DataContext = App.Beatmaps.GetRecentListFromDb().Transform(true).ToList();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
        }

        private void LblCreator_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void Recent_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RecentList.SelectedItem == null)
                return;
            if (e.OriginalSource is TextBlock)
                return;
            var searchInfo = (BeatmapViewModel)RecentList.SelectedItem;
            var map = App.Beatmaps.GetBeatmapsetsByFolder(searchInfo.FolderName)
                .FirstOrDefault(k => k.Version == searchInfo.Version);
            if (map != null)
            {
                ParentWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
                    map.BeatmapFileName));
                App.PlayerControl.RefreshPlayList(PlayerControl.FreshType.None, PlayListMode.RecentList);
            }
            else
            {
                //todo
            }
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
    }
}
