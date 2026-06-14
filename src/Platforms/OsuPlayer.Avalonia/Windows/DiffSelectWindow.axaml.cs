using System.Collections.Generic;
using Avalonia.Controls;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Windows;

public partial class DiffSelectWindow : Window
{
    public DiffSelectWindow()
    {
        InitializeComponent();
        ContentHost.BeatmapSelected += (_, beatmap) =>
        {
            SelectedBeatmap = beatmap;
            Close();
        };
    }

    public DiffSelectWindow(IReadOnlyList<Beatmap> entries) : this()
    {
        ContentHost.SetEntries(entries);
    }

    public Beatmap? SelectedBeatmap { get; private set; }
}
