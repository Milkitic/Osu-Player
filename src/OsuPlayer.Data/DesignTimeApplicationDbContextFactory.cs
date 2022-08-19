using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Milki.OsuPlayer.Shared;

namespace Milki.OsuPlayer.Data;

public class DesignTimeApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.EnableSensitiveDataLogging();
        var dataBases = Path.Combine(Constants.ApplicationDir, "databases");
        if (!Directory.Exists(dataBases))
        {
            Directory.CreateDirectory(dataBases);
        }

        var db = Path.Combine(dataBases, "application.db");
        optionsBuilder.UseSqlite("data source=" + db);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}