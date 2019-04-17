using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Migrations;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Common.Data.EF
{
    public class BeatmapDbContext : DbContext
    {
        static BeatmapDbContext()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<BeatmapDbContext, BeatmapMigrationConfiguration>(true));
        }

        public DbSet<Beatmap> Beatmaps { get; set; }

        public BeatmapDbContext() : base("name=beatmapDb")
        {
            //Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions
            //    .Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
            modelBuilder.Configurations.Add(new BeatmapDbConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
}
