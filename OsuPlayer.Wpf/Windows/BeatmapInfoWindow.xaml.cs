using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using Milky.OsuPlayer.Common.Player;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// BeatmapInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BeatmapInfoWindow : Window
    {
        private BeatmapContext _info;

        public BeatmapInfoWindow(BeatmapContext info)
        {
            InitializeComponent();
            DataContext = info;
            _info = info;
        }

        private void SLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://osu.ppy.sh/s/" + _info.BeatmapDetail.Metadata.BeatmapsetId);
        }

        private void BLink_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://osu.ppy.sh/b/" + _info.BeatmapDetail.Metadata.BeatmapId);
        }
    }
}
