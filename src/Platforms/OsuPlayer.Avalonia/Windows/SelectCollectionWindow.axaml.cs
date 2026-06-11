using System.Collections.Generic;
using Avalonia.Controls;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Windows;

public partial class SelectCollectionWindow : Window
{
    public SelectCollectionWindow()
    {
        InitializeComponent();
    }

    public SelectCollectionWindow(Beatmap entry) : this()
    {
        ContentHost.SetEntries(new List<Beatmap> { entry });
    }

    public SelectCollectionWindow(IList<Beatmap> entries) : this()
    {
        ContentHost.SetEntries(entries);
    }
}
