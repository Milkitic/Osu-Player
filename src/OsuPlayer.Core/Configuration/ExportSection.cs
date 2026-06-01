using System.Text.Json.Serialization;
using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Core.Configuration
{
    public class ExportSection
    {
        public string MusicPath { get; set; } = Domain.MusicPath;
        public string BgPath { get; set; } = Domain.BackgroundPath;
        [JsonPropertyName("NamingStyle")]
        public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
        [JsonPropertyName("SortStyle")]
        public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
    }
}
