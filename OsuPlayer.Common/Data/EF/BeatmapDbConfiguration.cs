using System.Data.Entity.ModelConfiguration;
using Milky.OsuPlayer.Common.Data.EF.Model;

namespace Milky.OsuPlayer.Common.Data.EF {
    public class BeatmapDbConfiguration : EntityTypeConfiguration<Beatmap>
    {
        public BeatmapDbConfiguration()
        {
            this.ToTable("beatmap");
            this.HasKey(m => m.Id);
        }
    }
}