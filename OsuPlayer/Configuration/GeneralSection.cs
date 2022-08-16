using System.IO;
using Milki.OsuPlayer.Common;

namespace Milki.OsuPlayer.Configuration;

public class GeneralSection
{
    public bool RunOnStartup { get; set; } = false;
    public string DbPath { get; set; }
    public string CustomSongsPath { get; set; } = Path.Combine(Domain.CurrentPath, "Songs");
    public bool? ExitWhenClosed { get; set; } = null;
}