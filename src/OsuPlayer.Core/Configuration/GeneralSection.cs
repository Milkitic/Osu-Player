using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace Milky.OsuPlayer.Core.Configuration;

public class GeneralSection
{
    public bool RunOnStartup { get; set; } = false;
    public string DbPath { get; set; }
    public string CustomSongsPath { get; set; } = Path.Combine(Domain.CurrentPath, "Songs");
    public bool? ExitWhenClosed { get; set; } = null;
    public bool FirstOpen { get; set; } = true;
    public bool IsNavigationCollapsed { get; set; }
    public Point? MiniLastPosition { get; set; }
    public Rectangle? MiniWorkingArea { get; set; }
}

public partial class InterfaceSection : ObservableObject
{
    [ObservableProperty]
    public partial bool MinimalMode { get; set; }

    public string Locale { get; set; }
}