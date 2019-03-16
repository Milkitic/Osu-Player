using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Migrations;
using System.Data.Entity;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;

namespace Milky.OsuPlayer.Common.Data.EF
{
    public class ApplicationDbContext : DbContext
    {
        static ApplicationDbContext()
        {
            //Database.SetInitializer(
            //    new MigrateDatabaseToLatestVersion<ApplicationDbContext, MigrationConfiguration>(true));
        }

        public DbSet<MapInfo> MapInfos { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionRelation> Relations { get; set; }

        public ApplicationDbContext() : base("name=sqlite")
        {
            //Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Conventions
            //    .Remove<System.Data.Entity.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
            //base.OnModelCreating(modelBuilder);
        }
    }
}