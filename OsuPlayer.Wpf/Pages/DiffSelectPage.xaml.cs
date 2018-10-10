using Milkitic.OsuPlayer.Data;
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

namespace Milkitic.OsuPlayer.Pages
{
    /// <summary>
    /// DiffSelectPage.xaml 的交互逻辑
    /// </summary>
    public partial class DiffSelectPage : Page
    {
        private readonly MainWindow _mainWindow;
        public Action Callback { private get; set; }

        public BeatmapViewModel SelectedMap;
        //private IEnumerable<BeatmapEntry> _entries;

        public DiffSelectPage(MainWindow mainWindow, IEnumerable<BeatmapEntry> entries)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            var viewModel = entries.Transform(true);
            DiffList.DataContext = viewModel;
            //_entries = entries;
        }

        private void BtnNowPlay_Click(object sender, RoutedEventArgs e)
        {
            SelectedMap = (BeatmapViewModel)DiffList.SelectedItem;
            Callback?.Invoke();
            Dispose();
        }

        private void BtnNextPlay_Click(object sender, RoutedEventArgs e)
        {

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
