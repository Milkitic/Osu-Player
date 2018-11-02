using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Data;
using Milkitic.OsuPlayer.ViewModels;
using Milkitic.OsuPlayer.Windows;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Collection = Milkitic.OsuPlayer.Data.Collection;

namespace Milkitic.OsuPlayer.Pages
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
            _mainWindow = mainWindow;
            _entry = entry;
            RefreshList();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void Dispose()
        {
            _mainWindow.FramePop.Navigate(null);
            GC.SuppressFinalize(this);
        }

        private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
        {
            FramePop.Navigate(new AddCollectionPage(_mainWindow, this));
        }

        public void RefreshList()
        {
            ViewModel.Collections = DbOperator.GetCollections().OrderByDescending(k => k.CreateTime).ToList();
        }

        private void CollectionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CollectionList.SelectedItem == null)
                return;
            Collection col = (Collection)CollectionList.SelectedItem;
            var entry = _entry;
            AddToCollection(col, entry);
            Dispose();
        }

        public static void AddToCollection(Collection col, BeatmapEntry entry)
        {
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                OsuFile osuFile =
                    new OsuFile(Path.Combine(Domain.OsuSongPath, entry.FolderName, entry.BeatmapFileName));
                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(Domain.OsuSongPath, entry.FolderName, osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        DbOperator.UpdateCollection(col);
                    }
                }
            }
            DbOperator.AddMapToCollection(entry, col);
        }

        private void BtnCollection_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var colId = (string)btn.Tag;
            var col = DbOperator.GetCollectionById(colId);
            AddToCollection(col, _entry);
            Dispose();
        }
    }
}
