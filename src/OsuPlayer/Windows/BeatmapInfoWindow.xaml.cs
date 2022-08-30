using System.Diagnostics;
using System.Windows;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Utils;

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
        ProcessUtils.StartWithShellExecute("https://osu.ppy.sh/s/" + _info.BeatmapSetId);
    }

    private void BLink_Click(object sender, RoutedEventArgs e)
    {
        ProcessUtils.StartWithShellExecute("https://osu.ppy.sh/b/" + _info.BeatmapId);
    }
}