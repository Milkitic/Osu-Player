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
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Milky.WpfApi.Commands;

namespace Milky.OsuPlayer.Pages
{
    public class RecentPlayPageVm : ViewModelBase
    {
        private AppDbOperator _appDbOperator = new AppDbOperator();
        private NumberableObservableCollection<BeatmapDataModel> _beatmaps;

        public NumberableObservableCollection<BeatmapDataModel> Beatmaps
        {
            get => _beatmaps;
            set
            {
                _beatmaps = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchByConditionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    WindowBase.GetCurrentFirst<MainWindow>()
                        .SwitchSearch
                        .CheckAndAction(page => ((SearchPage)page).Search((string)param));
                });
            }
        }

        public ICommand OpenSourceFolderCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _appDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    var fileName = beatmap.InOwnDb
                        ? Path.Combine(Domain.CustomSongPath, map.FolderName)
                        : Path.Combine(Domain.OsuSongPath, map.FolderName);
                    if (!Directory.Exists(fileName))
                    {
                        Notification.Show(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    Process.Start(fileName);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _appDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    Process.Start($"https://osu.ppy.sh/s/{map.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _appDbOperator.GetBeatmapByIdentifiable(beatmap);
                    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(map),
                        DialogOptionFactory.SelectCollectionOptions);
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _appDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    ExportPage.QueueEntry(map);
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _appDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    await PlayController.Default.PlayNewFile(map);
                    await Services.Get<PlayerList>()
                        .RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand(async param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    var map = _appDbOperator.GetBeatmapByIdentifiable(beatmap);
                    await PlayController.Default.PlayNewFile(map);
                    await Services.Get<PlayerList>()
                        .RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.RecentList);

                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand(param =>
                {
                    var beatmap = (BeatmapDataModel)param;
                    _appDbOperator.RemoveFromRecent(beatmap.GetIdentity());
                    Beatmaps.Remove(beatmap);
                    //await Services.Get<PlayerList>().RefreshPlayListAsync(PlayerList.FreshType.All, PlayListMode.Collection, _entries);
                });
            }
        }
    }

    /// <summary>
    /// RecentPlayPage.xaml 的交互逻辑
    /// </summary>
    public partial class RecentPlayPage : Page
    {
        private ObservableCollection<Beatmap> _recentBeatmaps;
        private readonly MainWindow _mainWindow;
        private AppDbOperator _appDbOperator = new AppDbOperator();
        private RecentPlayPageVm _viewModel;

        public RecentPlayPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _viewModel = (RecentPlayPageVm)DataContext;
        }

        public void UpdateList()
        {
            _recentBeatmaps = new ObservableCollection<Beatmap>(
                _appDbOperator.GetBeatmapsByMapInfo(_appDbOperator.GetRecentList(), TimeSortMode.PlayTime));
            _viewModel.Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(_recentBeatmaps.ToDataModelList(false));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
            var item = _viewModel.Beatmaps.FirstOrDefault(k =>
                k.GetIdentity().Equals(Services.Get<PlayerList>().CurrentInfo?.Identity));
            RecentList.SelectedItem = item;
        }

        private void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
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
            FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(GetSelected()),
                DialogOptionFactory.SelectCollectionOptions);
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
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.SongSource));
        }

        private void ItemSearchMapper_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.Creator));
        }

        private void ItemSearchArtist_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoArtist));
        }

        private void ItemSearchTitle_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            if (map == null) return;
            _mainWindow.SwitchSearch.CheckAndAction(page => ((SearchPage)page).Search(map.AutoTitle));
        }

        private void ItemSet_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            Process.Start($"https://osu.ppy.sh/b/{map.BeatmapId}");
        }

        private void ItemFolder_Click(object sender, RoutedEventArgs e)
        {
            var map = GetSelected();
            var dir = map.InOwnDb
                ? Path.Combine(Domain.CustomSongPath, map.FolderName)
                : Path.Combine(Domain.OsuSongPath, map.FolderName);
            if (!Directory.Exists(dir))
            {
                Notification.Show(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                return;
            }

            Process.Start(dir);
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
    }
}
