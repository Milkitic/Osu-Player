using System.IO;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class ExportSection
{
    public string DirBackground { get; set; } = Path.Combine(Environment.CurrentDirectory, "exports", "background");
    public string DirMusic { get; set; } = Path.Combine(Environment.CurrentDirectory, "exports", "music");
    public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
    public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
}