using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace GridViewTest
{
    /// <summary>
    /// GridViewControl.xaml 的交互逻辑
    /// </summary>
    public partial class GridViewControl : UserControl
    {
        private ObservableCollection<BeatmapDataModel> _beatmapDataModels;

        public GridViewControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _beatmapDataModels = new ObservableCollection<BeatmapDataModel>()
            {
                new BeatmapDataModel
                {
                    ArtistUnicode = "artist1",
                    TitleUnicode = "title1",
                    Creator = "creator1",
                    Version = "easy"
                },
                new BeatmapDataModel
                {
                    ArtistUnicode = "artist2",
                    TitleUnicode = "title2",
                    Creator = "creator2",
                    Version = "normal"
                }
            };
            ListView.ItemsSource = _beatmapDataModels;

            _beatmapDataModels.Add(new BeatmapDataModel
            {
                ArtistUnicode = "artist3",
                TitleUnicode = "title3",
                Creator = "creator3",
                Version = "hard"
            });
        }
    }
}
