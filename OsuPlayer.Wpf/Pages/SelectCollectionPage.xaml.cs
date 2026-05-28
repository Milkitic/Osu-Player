using System.Collections.Generic;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using OSharp.Beatmap;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// AddCollectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class SelectCollectionPage : Page
    {
        public SelectCollectionPageViewModel ViewModel { get; set; }

        private readonly MainWindow _mainWindow;
        private readonly IList<Beatmap> _entries;

        public SelectCollectionPage(Beatmap entry) : this(new[] { entry })
        {
        }

        public SelectCollectionPage(IList<Beatmap> entries)
        {
            InitializeComponent();
            ViewModel = (SelectCollectionPageViewModel)DataContext;
            _entries = entries;
            ViewModel.Entries = entries;
            _mainWindow = (MainWindow)Application.Current.MainWindow;
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
            using var db = new OsuPlayerDbContext();
            ViewModel.Collections = new ObservableCollection<CollectionViewModel>(
                CollectionViewModel.CopyFrom(db.GetCollections().OrderByDescending(k => k.CreateTime)));
        }

        public static async Task AddToCollectionAsync(Collection col, IList<Beatmap> entries)
        {
            if (entries.Count <= 0) return;
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
                        using var db = new OsuPlayerDbContext();
                        db.UpdateCollection(col);
                    }
                }
            }

            using (var db = new OsuPlayerDbContext())
            {
                db.AddMapsToCollection(entries, col);
            }
        }
    }
}
