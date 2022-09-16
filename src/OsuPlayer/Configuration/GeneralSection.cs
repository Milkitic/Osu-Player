#nullable enable

using System.IO;

namespace Milki.OsuPlayer.Configuration;

public class GeneralSection
{
    public bool? CloseBehavior { get; set; } = null;
    public string DirCustomSong { get; set; } = Path.Combine(Environment.CurrentDirectory, "Songs");
    public string DirOsuSong => Path.GetDirectoryName(PathOsuDb) is { } osuDir ? Path.Combine(osuDir, "Songs") : DirCustomSong;
    public bool IsRunOnStartup { get; set; } = false;
    public string? LastMigrateVersion { get; set; }
    public string? Locale { get; set; }
    public string? PathOsuDb { get; set; }
}