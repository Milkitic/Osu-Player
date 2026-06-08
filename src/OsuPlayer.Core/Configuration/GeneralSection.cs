#nullable enable

using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;
using Path = System.IO.Path;

namespace OsuPlayer.Core.Configuration;

public class GeneralSection
{
    public bool RunOnStartup { get; set; } = false;
    public string? DbPath { get; set; }
    public string CustomSongsPath { get; set; } = Path.Combine(Domain.CurrentPath, "Songs");
    public bool? ExitWhenClosed { get; set; } = null;
    public bool FirstOpen { get; set; } = true;
    public bool IsNavigationCollapsed { get; set; }
    public WindowPoint? MiniLastPosition { get; set; }
    public Rectangle? MiniWorkingArea { get; set; }
}

public partial class InterfaceSection : ObservableObject
{
    [ObservableProperty]
    public partial bool MinimalMode { get; set; }

    public string? Locale { get; set; }
}
