using System;
using System.IO;
using Milki.OsuPlayer.Shared.Models;
using Newtonsoft.Json;

namespace Milki.OsuPlayer.Configuration;

public class ExportSection
{
    public string MusicDir { get; set; } = Path.Combine(Environment.CurrentDirectory, "exports", "music");
    public string BackgroundDir { get; set; } = Path.Combine(Environment.CurrentDirectory, "exports", "background");
    [JsonProperty("NamingStyle")]
    public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
    [JsonProperty("SortStyle")]
    public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
}