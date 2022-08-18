using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Coosu.Beatmap;
using Coosu.Database.DataTypes;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.ViewModels;
using Milki.OsuPlayer.Windows;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.UserControls
{
    /// <summary>
    /// SelectCollectionControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionControl : UserControl
    {
        private readonly IList<Beatmap> _beatmaps;
        private SelectCollectionPageViewModel _viewModel;
        private FrontDialogOverlay _overlay;

        public SelectCollectionControl(Beatmap beatmap) : this(new[] { beatmap })
        {
        }

        public SelectCollectionControl(IList<Beatmap> beatmaps)
        {
            _beatmaps = beatmaps;
            InitializeComponent();
        }

        private async void SelectCollectionControl_OnInitialized(object? sender, EventArgs e)
        {
            _viewModel = (SelectCollectionPageViewModel)DataContext;
            _viewModel.PlayItems = _beatmaps;
            await RefreshList();
            _overlay = FrontDialogOverlay.Default.GetOrCreateSubOverlay();
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            var addCollectionControl = new AddCollectionControl();
            _overlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, async (obj, args) =>
            {
                await using var dbContext = new ApplicationDbContext();
                await dbContext.AddCollection(addCollectionControl.CollectionName.Text);// todo: exist
                await WindowEx.GetCurrentFirst<MainWindow>().UpdateCollections();

                await RefreshList();
            });
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            FrontDialogOverlay.Default.RaiseOk();
        }

        private async Task RefreshList()
        {
            await using var dbContext = new ApplicationDbContext();
            _viewModel.PlayLists = new ObservableCollection<Collection>(await dbContext.GetCollections());
        }

        public static async Task<bool> AddToCollectionAsync([NotNull] Collection col, IList<Beatmap> beatmaps)
        {
            var controller = Service.Get<ObservablePlayController>();
            await using var dbContext = new ApplicationDbContext();
            if (beatmaps == null || beatmaps.Count <= 0) return false;
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                var first = beatmaps[0];
                var dir = first.GetFolder(out var isFromDb, out var freePath);
                var filePath = isFromDb ? Path.Combine(dir, first.BeatmapFileName) : freePath;
                LocalOsuFile osuFile;
                try
                {
                    osuFile = await OsuFile.ReadFromFileAsync(filePath, options =>
                    {
                        options.IncludeSection("Events");
                        options.IgnoreSample();
                        options.IgnoreStoryboard();
                    });
                }
                catch (Exception e)
                {
                    return false;
                }

                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        await dbContext.AddOrUpdateCollection(col);
                    }
                }
            }

            await dbContext.AddBeatmapsToCollection(beatmaps, col);
            if (col.IsDefault)
            {
                foreach (var beatmap in beatmaps)
                {
                    if (controller.PlayList.CurrentInfo.Beatmap.Equals(beatmap))
                    {
                        controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = true;
                        break;
                    }
                }
            }

            return true;
        }
    }
}
