using Milky.OsuPlayer.Shared;
using Milky.OsuPlayer.Shared.Models;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class ExportSection
    {
        public string MusicPath { get; set; } = Domain.MusicPath;
        public string BgPath { get; set; } = Domain.BackgroundPath;
        [JsonProperty("NamingStyle")]
        public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
        [JsonProperty("SortStyle")]
        public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
    }
}