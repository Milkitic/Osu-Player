using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap;
using osu_database_reader.Components.Beatmaps;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Collection = Milky.OsuPlayer.Common.Data.EF.Model.Collection;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// AddCollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionPage : Page
    {
        public SelectCollectionPageViewModel ViewModel { get; set; }

        private readonly MainWindow _mainWindow;
        private readonly BeatmapEntry _entry;

        public SelectCollectionPage(MainWindow mainWindow, BeatmapEntry entry)
        {
            InitializeComponent();
            ViewModel = (SelectCollectionPageViewModel)DataContext;
            _entry = entry;
            ViewModel.Entry = entry;
            _mainWindow = mainWindow;
            RefreshList();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            _mainWindow.FramePop.Navigate(null);
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(_mainWindow, this));
        }

        public void RefreshList()
        {
            ViewModel.Collections = new ObservableCollection<CollectionViewModel>(
                CollectionViewModel.CopyFrom(DbOperate.GetCollections().OrderByDescending(k => k.CreateTime)));
        }

        public static async Task AddToCollectionAsync(Collection col, BeatmapEntry entry)
        {
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                var osuFile = await
                     OsuFile.ReadFromFileAsync(Path.Combine(Domain.OsuSongPath, entry.FolderName, entry.BeatmapFileName));
                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(Domain.OsuSongPath, entry.FolderName, osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        DbOperate.UpdateCollection(col);
                    }
                }
            }
            DbOperate.AddMapToCollection(entry, col);
        }
    }
}
