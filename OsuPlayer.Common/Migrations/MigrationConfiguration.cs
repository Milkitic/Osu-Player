using System.Data.Entity.Migrations;
using System.Data.SQLite.EF6.Migrations;
using Milky.OsuPlayer.Common.Data.EF;

namespace Milky.OsuPlayer.Common.Migrations
{

    internal sealed class MigrationConfiguration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public MigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            SetSqlGenerator("System.Data.SQLite", new SQLiteMigrationSqlGenerator());
        }

        protected override void Seed(ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
