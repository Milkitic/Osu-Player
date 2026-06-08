using System.Text.Json.Serialization;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Core.Configuration;

public class ExportSection
{
    public string MusicPath { get; set; } = AppPaths.Current.MusicPath;
    public string BgPath { get; set; } = AppPaths.Current.BackgroundPath;
    [JsonPropertyName("NamingStyle")]
    public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
    [JsonPropertyName("SortStyle")]
    public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
}
