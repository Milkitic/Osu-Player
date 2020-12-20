using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio.Playlist;
using System.Windows;

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
            ProcessLegacy.StartLegacy("https://osu.ppy.sh/s/" + _info.BeatmapDetail.Metadata.BeatmapsetId);
        }

        private void BLink_Click(object sender, RoutedEventArgs e)
        {
            ProcessLegacy.StartLegacy("https://osu.ppy.sh/b/" + _info.BeatmapDetail.Metadata.BeatmapId);
        }
    }
}
