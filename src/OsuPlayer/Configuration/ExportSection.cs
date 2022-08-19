using System.IO;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Configuration;

public class ExportSection
{
    public string MusicDir { get; set; } = Path.Combine(Environment.CurrentDirectory, "exports", "music");
    public string BackgroundDir { get; set; } = Path.Combine(Environment.CurrentDirectory, "exports", "background");
    public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
    public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
}