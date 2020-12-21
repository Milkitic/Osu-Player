using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Pages;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.NotificationComponent;
using Milky.OsuPlayer.Windows;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xaml;

namespace Milky.OsuPlayer.UserControls.ItemsControls
{
    /// <summary>
    /// BeatmapDataGrid.xaml 的交互逻辑
    /// </summary>
    public partial class BeatmapDataGrid : BeatmapItemsControlBase
    {

        public BeatmapDataGrid()
        {
            InitializeComponent();
        }
    }

    public class BeatmapItemsControlBase : UserControl
    {
        public static readonly DependencyProperty GroupListProperty = DependencyProperty.Register(
            "GroupList",
            typeof(ObservableCollection<OrderedBeatmapGroup>),
            typeof(BeatmapItemsControlBase),
            new FrameworkPropertyMetadata(null, GroupListChanged)
        );

        public ObservableCollection<OrderedBeatmapGroup> GroupList
        {
            get => (ObservableCollection<OrderedBeatmapGroup>)GetValue(GroupListProperty);
            set => SetValue(GroupListProperty, value);
        }

        public static readonly DependencyProperty BeatmapListProperty = DependencyProperty.Register(
            "BeatmapList",
            typeof(ObservableCollection<OrderedBeatmap>),
            typeof(BeatmapItemsControlBase),
            new FrameworkPropertyMetadata(null, DataListChanged)
        );

        public ObservableCollection<OrderedBeatmap> BeatmapList
        {
            get => (ObservableCollection<OrderedBeatmap>)GetValue(BeatmapListProperty);
            set => SetValue(BeatmapListProperty, value);
        }

        public static readonly DependencyProperty IsGroupModeProperty = DependencyProperty.Register(
            "IsGroupMode",
            typeof(bool),
            typeof(BeatmapItemsControlBase),
            new FrameworkPropertyMetadata(null)
        );

        public bool IsGroupMode
        {
            get => (bool)GetValue(IsGroupModeProperty);
            private set => SetValue(IsGroupModeProperty, value);
        }

        //public static readonly DependencyProperty IsAlreadyGroupedProperty = DependencyProperty.Register(
        //    "IsAlreadyGrouped",
        //    typeof(bool),
        //    typeof(BeatmapItemsControlBase),
        //    new FrameworkPropertyMetadata(null)
        //);

        //public bool IsAlreadyGrouped
        //{
        //    get => (bool)GetValue(IsAlreadyGroupedProperty);
        //    set => SetValue(IsAlreadyGroupedProperty, value);
        //}

        private static void GroupListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BeatmapDataGrid grid)
            {
                grid.IsGroupMode = true;
            }
        }

        private static void DataListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BeatmapDataGrid grid)
            {
                grid.IsGroupMode = false;
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
                return new DelegateCommand<OrderedBeatmap>(async param =>
                {
                    var beatmap = (Beatmap)param;
                    if (IsGroupMode)
                    {
                        beatmap = await GetHighestSrBeatmap(beatmap);
                        if (beatmap == null) return;
                    }

                    var folderName = beatmap.GetFolder(out _, out _);
                    if (!Directory.Exists(folderName))
                    {
                        Notification.Push(@"所选文件不存在，可能没有及时同步。请尝试手动同步osuDB后重试。");
                        return;
                    }

                    ProcessLegacy.StartLegacy(folderName);
                });
            }
        }

        public ICommand OpenScorePageCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async param =>
                {
                    var beatmap = (Beatmap)param;
                    if (IsGroupMode)
                    {
                        beatmap = await GetHighestSrBeatmap(beatmap);
                        if (beatmap == null) return;
                    }

                    ProcessLegacy.StartLegacy($"https://osu.ppy.sh/s/{beatmap.BeatmapSetId}");
                });
            }
        }

        public ICommand SaveCollectionCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async param =>
                {
                    var beatmap = (Beatmap)param;

                    await using var dbContext = new ApplicationDbContext();
                    var beatmaps = await dbContext.GetBeatmapsFromFolder(beatmap.FolderNameOrPath, beatmap.InOwnDb);

                    var control = new DiffSelectControl(
                        beatmaps, (selected, arg) =>
                        {
                            arg.Handled = true;
                            FrontDialogOverlay.Default.ShowContent(
                                new SelectCollectionControl(selected),
                                DialogOptionFactory.SelectCollectionOptions
                            );
                        });
                    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
                });
            }
        }

        public ICommand ExportCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async param =>
                {
                    var beatmap = (Beatmap)param;
                    if (IsGroupMode)
                    {
                        beatmap = await GetHighestSrBeatmap(beatmap);
                        if (beatmap == null) return;
                    }

                    ExportPage.QueueBeatmap(beatmap);
                });
            }
        }

        public ICommand DirectPlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async param =>
                {
                    var beatmap = (Beatmap)param;
                    if (IsGroupMode)
                    {
                        beatmap = await GetHighestSrBeatmap(beatmap);
                        if (beatmap == null) return;
                    }

                    var controller = Service.Get<ObservablePlayController>();
                    await controller.PlayNewAsync(beatmap);
                });
            }
        }

        public ICommand PlayCommand
        {
            get
            {
                return new DelegateCommand<OrderedBeatmap>(async param =>
                {
                    if (!IsGroupMode)
                    {
                        DirectPlayCommand.Execute(param);
                        return;
                    }

                    var beatmap = (Beatmap)param;
                    await using var dbContext = new ApplicationDbContext();
                    var beatmaps = await dbContext.GetBeatmapsFromFolder(beatmap.FolderNameOrPath, beatmap.InOwnDb);

                    var control = new DiffSelectControl(
                        beatmaps, async (selected, arg) =>
                        {
                            var controller = Service.Get<ObservablePlayController>();
                            await controller.PlayNewAsync(selected, true);
                        });
                    FrontDialogOverlay.Default.ShowContent(control, DialogOptionFactory.DiffSelectOptions);
                });
            }
        }

        private async Task<Beatmap> GetHighestSrBeatmap(Beatmap beatmap)
        {
            if (beatmap == null) return null;

            await using var dbContext = new ApplicationDbContext();
            var map = (await dbContext.GetBeatmapsFromFolder(beatmap.FolderNameOrPath, beatmap.InOwnDb))
                .GetHighestDiff();
            return map;
        }
    }

    [MarkupExtensionReturnType(typeof(BeatmapDataGrid))]
    class RootBeatmapDataGrid : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) =>
            ((IRootObjectProvider)serviceProvider.GetService(typeof(IRootObjectProvider)))?.RootObject;
    }
}
