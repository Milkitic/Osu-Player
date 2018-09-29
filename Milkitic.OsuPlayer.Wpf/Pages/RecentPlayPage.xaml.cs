using Milkitic.OsuPlayer.Wpf.Data;
using Milkitic.OsuPlayer.Wpf.Models;
using MS.Internal.AppModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Milkitic.OsuPlayer.Wpf.Pages
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
                ParentWindow.FillPlayList(false, false, PlayListMode.RecentList);
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
            ParentWindow.FillPlayList(true, true, PlayListMode.RecentList);
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
