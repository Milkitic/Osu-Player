using Milky.OsuPlayer.Common.Metadata;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class ExportControl
    {
        public string MusicPath { get; set; } = Domain.MusicPath;
        public string BgPath { get; set; } = Domain.BackgroundPath;
        public NamingStyle NamingStyle { get; set; } = NamingStyle.ArtistTitle;
        public SortStyle SortStyle { get; set; } = SortStyle.Artist;

    }
}