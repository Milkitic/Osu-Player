using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
using Milky.WpfApi.Collections;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Models;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// DiffSelectPage.xaml 的交互逻辑
    /// </summary>
    public partial class DiffSelectPage : Page
    {
        private readonly MainWindow _mainWindow;

        public Action Callback
        {
            private get { return ViewModel.Callback; }
            set { ViewModel.Callback = value; }
        }

        public BeatmapDataModel SelectedMap => ViewModel.SelectedMap;
        public DiffSelectPageViewModel ViewModel { get; set; }

        public DiffSelectPage(MainWindow mainWindow, IEnumerable<Beatmap> entries)
        {
            InitializeComponent();

            ViewModel = (DiffSelectPageViewModel)DataContext;
            _mainWindow = mainWindow;
            ViewModel.Entries = entries;
            ViewModel.DataModels = new NumberableObservableCollection<BeatmapDataModel>(ViewModel.Entries.ToDataModels(false));
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
            _mainWindow.FramePop.Navigate(null);
        }

        private void Dispose()
        {
         
        }
    }
}
