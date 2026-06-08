using Microsoft.Win32;

namespace OsuPlayer.Utils;

internal static class OsuDatabaseDialog
{
    public static bool? Browse(out string path)
    {
        var dialog = new OpenFileDialog
        {
            Title = @"请选择osu所在目录内的""osu!.db""",
            Filter = @"Beatmap Database|osu!.db"
        };

        var result = dialog.ShowDialog();
        path = dialog.FileName;
        return result;
    }
}
