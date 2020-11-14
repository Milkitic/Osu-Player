using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Presentation.ObjectModel;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milky.OsuPlayer.Pages
{
    public class RecentPlayPageVm : VmBase
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
        private readonly SafeDbOperator _safeDbOperator = new SafeDbOperator();
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
                    WindowEx.GetCurrentFirst<MainWindow>()
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
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);

                    if (map == null) return;
                    var folder = beatmap.GetFolder(out _, out _);
                    if (!Directory.Exists(folder))
                    {
                        Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    Process.Start(folder);
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
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
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
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
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
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
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
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    await _controller.PlayNewAsync(map);
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
                    var map = _safeDbOperator.GetBeatmapByIdentifiable(beatmap);
                    if (map == null) return;
                    await _controller.PlayNewAsync(map);
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
                    if (_safeDbOperator.TryRemoveFromRecent(beatmap.GetIdentity()))
                    {
                        Beatmaps.Remove(beatmap);
                    }
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
        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
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
                SafeDbOperator.GetBeatmapsByMapInfo(SafeDbOperator.GetRecentList(), TimeSortMode.PlayTime));
            _viewModel.Beatmaps = new NumberableObservableCollection<BeatmapDataModel>(_recentBeatmaps.ToDataModelList(false));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateList();
            var item = _viewModel.Beatmaps.FirstOrDefault(k =>
                k.GetIdentity().Equals(_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
            RecentList.SelectedItem = item;
        }

        private void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private void BtnDelAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveAll"), _mainWindow.Title, MessageBoxButton.OKCancel,
                MessageBoxImage.Exclamation);
            if (result == MessageBoxResult.OK)
            {
                if (SafeDbOperator.TryClearRecent())
                    UpdateList();
            }
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            await _controller.PlayList.SetSongListAsync(_recentBeatmaps, true);
        }

        private async void PlaySelected()
        {
            var map = GetSelected();
            if (map == null) return;

            await _controller.PlayNewAsync(map);
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
