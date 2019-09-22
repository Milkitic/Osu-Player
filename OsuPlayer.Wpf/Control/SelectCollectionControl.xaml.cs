using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi;
using OSharp.Beatmap;

namespace Milky.OsuPlayer.Control
{
    /// <summary>
    /// SelectCollectionControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionControl : UserControl
    {
        private SelectCollectionPageViewModel _viewModel;
        private AppDbOperator _appDbOperator = new AppDbOperator();
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
                _appDbOperator.AddCollection(addCollectionControl.CollectionName.Text);

                WindowBase.GetCurrentFirst<MainWindow>().UpdateCollections();
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
                CollectionViewModel.CopyFrom(_appDbOperator.GetCollections().OrderByDescending(k => k.CreateTime)));
        }

        public static async Task AddToCollectionAsync(Collection col, IList<Beatmap> entries)
        {
            var appDbOperator = new AppDbOperator();
            if (entries == null || entries.Count <= 0) return;
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                var first = entries[0];
                var osuFile =
                    await OsuFile.ReadFromFileAsync(Path.Combine(Domain.OsuSongPath, first.FolderName,
                        first.BeatmapFileName));
                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(Domain.OsuSongPath, first.FolderName,
                        osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        appDbOperator.UpdateCollection(col);
                    }
                }
            }

            appDbOperator.AddMapsToCollection(entries, col);
        }
    }
}
