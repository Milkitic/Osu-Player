using System.Diagnostics;
using System.Windows;
using Milki.OsuPlayer.Audio.Playlist;

namespace Milki.OsuPlayer.Windows
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
