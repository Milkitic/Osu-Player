using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Data;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly MainWindow _mainWindow;
        private readonly BeatmapEntry _entry;

        public SelectCollectionPage(MainWindow mainWindow, BeatmapEntry entry)
        {
            _mainWindow = mainWindow;
            _entry = entry;
            InitializeComponent();
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
            var list = (List<Collection>)DbOperator.GetCollections();
            list.Reverse();
            CollectionList.DataContext = list;
        }

        private void CollectionList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CollectionList.SelectedItem == null)
                return;
            Collection col = (Collection)CollectionList.SelectedItem;
            if (string.IsNullOrEmpty(col.ImagePath))
            {
                OsuFile osuFile =
                    new OsuFile(Path.Combine(Domain.OsuSongPath, _entry.FolderName, _entry.BeatmapFileName));
                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(Domain.OsuSongPath, _entry.FolderName, osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        DbOperator.UpdateCollection(col);
                    }
                }
            }
            DbOperator.AddMapToCollection(_entry, col);
            Dispose();
        }
    }
}
