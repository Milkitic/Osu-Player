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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Milky.OsuPlayer.Pages
{
    public class RecentPlayPageVm : VmBase
    {
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
        private ObservableCollection<OrderedBeatmap> _beatmaps;

        public ObservableCollection<OrderedBeatmap> Beatmaps
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
                return new DelegateCommand<string>(keyword =>
                {
                    WindowEx.GetCurrentFirst<MainWindow>()
                        .SwitchSearch
                        .CheckAndAction(page => ((SearchPage)page).Search(keyword));
                });
            }
        }

        public ICommand OpenSourceFolderCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    var folder = orderedModel.Model.GetFolder(out _, out _);
                    if (!Directory.Exists(folder))
                    {
                        Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    ProcessLegacy.StartLegacy(folder);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    ProcessLegacy.StartLegacy($"https://osu.ppy.sh/s/{orderedModel.Model.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(orderedModel),
                        DialogOptionFactory.SelectCollectionOptions);
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(orderedModel =>
                {
                    if (orderedModel == null) return;
                    ExportPage.QueueBeatmap(orderedModel);
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async orderedModel =>
                {
                    if (orderedModel == null) return;
                    await _controller.PlayNewAsync(orderedModel);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async orderedModel =>
                {
                    if (orderedModel == null) return;
                    await _controller.PlayNewAsync(orderedModel);
                });
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async map =>
                {
                    await using var appDbContext = new ApplicationDbContext();
                    await appDbContext.RemoveBeatmapFromRecent(map);
                    {
                        Beatmaps.Remove(map);
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
        private readonly MainWindow _mainWindow;
        private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
        private RecentPlayPageVm _viewModel;

        public RecentPlayPage()
        {
            InitializeComponent();
            _mainWindow = (MainWindow)Application.Current.MainWindow;
            _viewModel = (RecentPlayPageVm)DataContext;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateList();
            var item = _viewModel.Beatmaps.FirstOrDefault(k =>
                k.Model.Equals(_controller.PlayList.CurrentInfo?.Beatmap)
            );
            RecentList.SelectedItem = item;
        }

        private void RecentListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            PlaySelected();
        }

        private async void BtnDelAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveAll"), _mainWindow.Title, MessageBoxButton.OKCancel,
                MessageBoxImage.Exclamation);
            if (result != MessageBoxResult.OK) return;

            await using var appDbContext = new ApplicationDbContext();
            await appDbContext.ClearRecent();
            _viewModel.Beatmaps.Clear();
        }

        private async Task UpdateList()
        {
            await using var appDbContext = new ApplicationDbContext();
            var queryResult = await appDbContext.GetRecentList();
            // todo: pagination
            _viewModel.Beatmaps = new ObservableCollection<OrderedBeatmap>(queryResult.Collection.AsOrderedBeatmap());
        }

        private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
        {
            await using var appDbContext = new ApplicationDbContext();
            var paginationQueryResult = await appDbContext.GetRecentList(0, int.MaxValue);
            await _controller.PlayList.SetSongListAsync(paginationQueryResult.Collection, true);
        }

        private async void PlaySelected()
        {
            var map = (Beatmap)RecentList.SelectedItem;
            if (map == null) return;

            await _controller.PlayNewAsync(map);
        }
    }
}
