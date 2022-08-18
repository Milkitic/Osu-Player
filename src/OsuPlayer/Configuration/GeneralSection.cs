#nullable enable

using System;
using System.IO;

namespace Milki.OsuPlayer.Configuration;

public class GeneralSection
{
    public bool RunOnStartup { get; set; } = false;
    public string? DbPath { get; set; }
    public string CustomSongDir { get; set; } = Path.Combine(Environment.CurrentDirectory, "Songs");
    public bool? ExitWhenClosed { get; set; } = null;
    public string? Locale { get; set; }

    public string OsuSongDir =>
        Path.GetDirectoryName(DbPath) is { } osuDir ? Path.Combine(osuDir, "Songs") : CustomSongDir;
}