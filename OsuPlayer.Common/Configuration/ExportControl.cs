using Milky.OsuPlayer.Common.Metadata;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class ExportControl
    {
        public string MusicPath { get; set; } = Domain.MusicPath;
        public string BgPath { get; set; } = Domain.BackgroundPath;
        [JsonProperty("NamingStyle")]
        public ExportNamingStyle ExportNamingStyle { get; set; } = ExportNamingStyle.ArtistTitle;
        [JsonProperty("SortStyle")]
        public ExportGroupStyle ExportGroupStyle { get; set; } = ExportGroupStyle.Artist;
    }
}