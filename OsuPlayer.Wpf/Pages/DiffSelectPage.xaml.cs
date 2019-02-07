using Milky.OsuPlayer.Data;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
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
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages
{
    /// <summary>
    /// DiffSelectPage.xaml 的交互逻辑
    /// </summary>
    public partial class DiffSelectPage : Page
    {
        private readonly MainWindow _mainWindow;
        private Action _callback;

        public Action Callback
        {
            private get { return ViewModel.Callback; }
            set { ViewModel.Callback = value; }
        }

        public BeatmapDataModel SelectedMap => ViewModel.SelectedMap;
        public DiffSelectPageViewModel ViewModel { get; set; }

        public DiffSelectPage(MainWindow mainWindow, IEnumerable<BeatmapEntry> entries)
        {
            InitializeComponent();

            ViewModel = (DiffSelectPageViewModel)DataContext;
            _mainWindow = mainWindow;
            ViewModel.Entries = entries;
            ViewModel.DataModels = ViewModel.Entries.Transform(true);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Dispose();
            _mainWindow.FramePop.Navigate(null);
        }

        private void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
