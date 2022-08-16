using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Shared.Models;
using Newtonsoft.Json;

namespace Milki.OsuPlayer.Configuration;

public class ExportSection
{
    public string MusicPath { get; set; } = Domain.MusicPath;
    public string BgPath { get; set; } = Domain.BackgroundPath;
    [JsonProperty("NamingStyle")]
    public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
    [JsonProperty("SortStyle")]
    public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
}