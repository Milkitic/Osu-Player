using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Models;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// StoryboardPage.xaml 的交互逻辑
    /// </summary>
    public partial class StoryboardPage : Page
    {
        private readonly MainWindow _mainWindow;
        private StoryboardVm _viewModel;

        private AppDbOperator _appDbOperator = new AppDbOperator();
        public StoryboardPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _viewModel = StoryboardVm.Default;
            DataContext = StoryboardVm.Default;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //if(StoryboardVm.Default.BeatmapModels)
            if (AppSettings.Default.General.SbScanned)
            {
                ListScene.Visibility = Visibility.Visible;
                ScanScene.Visibility = Visibility.Collapsed;
                ScanScene.IsEnabled = false;

                var allMapSbFullInfos = _appDbOperator.GetAllMapSbFullInfos();
                var versions = _appDbOperator.GetAllMapSbInfos();
                StoryboardVm.Default.StoryboardDataModels = new ObservableCollection<StoryboardDataModel>();
                foreach (var storyboardFullInfo in allMapSbFullInfos)
                {
                    var storyboardDataModel = new StoryboardDataModel
                    {
                        Folder = storyboardFullInfo.InOwnFolder
                            ? Path.Combine(Domain.CustomSongPath, storyboardFullInfo.FolderName)
                            : Path.Combine(Domain.OsuSongPath, storyboardFullInfo.FolderName),
                        ContainsVersions = new List<string>(),
                        ThumbPath = storyboardFullInfo.SbThumbPath,
                        ThumbVideoPath = storyboardFullInfo.SbThumbVideoPath
                    };

                    var v = versions.Where(k =>
                        k.InOwnFolder == storyboardFullInfo.InOwnFolder &&
                        k.FolderName == storyboardFullInfo.FolderName).ToList();

                    if (v.Count > 0)
                    {
                        storyboardDataModel.DiffHasStoryboardOnly = false;
                        foreach (var fullInfo in v)
                        {
                            storyboardDataModel.ContainsVersions.Add(fullInfo.Version);
                        }
                    }

                    StoryboardVm.Default.StoryboardDataModels.Add(storyboardDataModel);
                }
                //StoryboardVm.Default.BeatmapModels = allMapSbFullInfos;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // todo
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            StoryboardVm.Default.IsScanning = true;

            await StoryboardVm.Default.ScanBeatmap().ConfigureAwait(false);

            StoryboardVm.Default.IsScanning = false;
            AppSettings.Default.General.SbScanned = true;
            AppSettings.SaveDefault();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var sb = (StoryboardDataModel)((Button)sender).Tag;
            var folder = new DirectoryInfo(sb.Folder);
            if (folder.Exists)
            {
                var beatmaps = _appDbOperator.GetBeatmapsFromFolder(folder.Name);
                if (beatmaps.Count > 0)
                {
                    await PlayController.Default.PlayNewFile(beatmaps.OrderByDescending(k => k.DiffSrNoneStandard)
                        .First());
                    var osb = folder.GetFiles("*.osb");
                    if (osb.Length > 0)
                    {
                        var sbFile = osb[0];
                        await Task.Delay(2000);
                        AnimationControl.Default.MyStoryboardPlayer.SetPlayer(
                            new TimelinePlayer(ComponentPlayer.Current.HitsoundPlayer));
                        AnimationControl.Default.SetStoryboard(sbFile.FullName);
                    }
                }
            }
        }
    }
}
