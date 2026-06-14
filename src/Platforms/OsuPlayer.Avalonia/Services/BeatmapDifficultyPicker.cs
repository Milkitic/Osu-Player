using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OsuPlayer.Data.Models;
using OsuPlayer.Windows;

namespace OsuPlayer.Services;

public sealed class BeatmapDifficultyPicker : IBeatmapDifficultyPicker
{
    public async Task<Beatmap?> PickAsync(IReadOnlyList<Beatmap> beatmaps)
    {
        if (beatmaps.Count == 0)
        {
            return null;
        }

        var dialog = new DiffSelectWindow(beatmaps);
        var owner = GetMainWindow();
        if (owner != null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            var completion = new TaskCompletionSource();
            dialog.Closed += (_, _) => completion.TrySetResult();
            dialog.Show();
            await completion.Task;
        }

        return dialog.SelectedBeatmap;
    }

    private static Window? GetMainWindow()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
