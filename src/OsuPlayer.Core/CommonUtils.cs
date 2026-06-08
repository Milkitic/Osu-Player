using System;
using System.Windows;
using Microsoft.Win32;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Core;

public static class CommonUtils
{
    public static bool? BrowseDb(out string path)
    {
        var fbd = new OpenFileDialog
        {
            Title = @"请选择osu所在目录内的""osu!.db""",
            Filter = @"Beatmap Database|osu!.db"
        };
        var result = fbd.ShowDialog();
        path = fbd.FileName;
        return result;
    }

    public static Duration GetDuration(TimeSpan ts)
    {
        if (AppSettings.Default == null) return TimeSpan.Zero;
        if (AppSettings.Default.Interface.MinimalMode)
            return new Duration(TimeSpan.Zero);
        return new Duration(ts);
    }
}
