using System.Diagnostics;
using System.Windows;
using Milki.OsuPlayer.Data.Models;

namespace Milki.OsuPlayer.Windows;

/// <summary>
/// BeatmapInfoWindow.xaml 的交互逻辑
/// </summary>
public partial class BeatmapInfoWindow : Window
{
    private readonly PlayItemDetail _info;

    public BeatmapInfoWindow(PlayItemDetail info)
    {
        _info = info;

        InitializeComponent();
        DataContext = info;
    }

    private void SLink_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("https://osu.ppy.sh/s/" + _info.BeatmapSetId);
    }

    private void BLink_Click(object sender, RoutedEventArgs e)
    {
        Process.Start("https://osu.ppy.sh/b/" + _info.BeatmapId);
    }
}