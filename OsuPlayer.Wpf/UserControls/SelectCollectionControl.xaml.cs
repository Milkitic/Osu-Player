using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Coosu.Beatmap;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Annotations;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.UserControls
{
    /// <summary>
    /// SelectCollectionControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionControl : UserControl
    {
        private SelectCollectionPageViewModel _viewModel;
        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();
        private FrontDialogOverlay _overlay;

        public SelectCollectionControl(Beatmap entry) : this(new[] { entry })
        {
        }

        public SelectCollectionControl(IList<Beatmap> entries)
        {
            InitializeComponent();
            _viewModel = (SelectCollectionPageViewModel)DataContext;
            _viewModel.Entries = entries;
            RefreshList();
            _overlay = FrontDialogOverlay.Default.GetOrCreateSubOverlay();
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            var addCollectionControl = new AddCollectionControl();
            _overlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, (obj, args) =>
            {
                if (!SafeDbOperator.TryAddCollection(addCollectionControl.CollectionName.Text))
                    return;

                WindowEx.GetCurrentFirst<MainWindow>().UpdateCollections();
                RefreshList();
            });
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            FrontDialogOverlay.Default.RaiseOk();
        }

        private void RefreshList()
        {
            _viewModel.Collections = new ObservableCollection<CollectionViewModel>(
                CollectionViewModel.CopyFrom(SafeDbOperator.GetCollections().OrderByDescending(k => k.CreateTime)));
        }

        public static async Task<bool> AddToCollectionAsync([NotNull] Collection col, IList<Beatmap> entries)
        {
            var controller = Service.Get<ObservablePlayController>();
            var appDbOperator = new AppDbOperator();
            if (entries == null || entries.Count <= 0) return false;
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                var first = entries[0];
                var dir = first.GetFolder(out var isFromDb, out var freePath);
                var filePath = isFromDb ? Path.Combine(dir, first.BeatmapFileName) : freePath;
                try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(filePath, options =>
                    {
                        options.IncludeSection("Events");
                        options.IgnoreSample();
                        options.IgnoreStoryboard();
                    });
                    if (osuFile.Events.BackgroundInfo != null)
                    {
                        var imgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
                        if (File.Exists(imgPath))
                        {
                            col.ImagePath = imgPath;
                            appDbOperator.UpdateCollection(col);
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }

            appDbOperator.AddMapsToCollection(entries, col);
            foreach (var beatmap in entries)
            {
                if (!controller.PlayList.CurrentInfo.Beatmap.GetIdentity().Equals(beatmap.GetIdentity()) ||
                    !col.LockedBool) continue;
                controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
                break;
            }

            return true;
        }
    }
}
