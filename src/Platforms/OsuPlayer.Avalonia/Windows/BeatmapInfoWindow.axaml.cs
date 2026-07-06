using System;
using System.Diagnostics;
using Avalonia.Controls;
using OsuPlayer.Playback.Playlist;

namespace OsuPlayer.Windows;

public partial class BeatmapInfoWindow : Window
{
    private readonly BeatmapContext _info;

    public BeatmapInfoWindow(BeatmapContext info)
    {
        InitializeComponent();
        DataContext = info;
        _info = info;
    }

    private void SLink_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenUrl($"https://osu.ppy.sh/s/{_info.BeatmapDetail.Metadata.BeatmapsetId}");
    }

    private void BLink_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OpenUrl($"https://osu.ppy.sh/b/{_info.BeatmapDetail.Metadata.BeatmapId}");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore launch failures on restricted environments.
        }
    }
}
