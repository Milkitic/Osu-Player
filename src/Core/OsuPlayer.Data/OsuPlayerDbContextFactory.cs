using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OsuPlayer.Data;

public sealed class OsuPlayerDbContextFactory : IDesignTimeDbContextFactory<OsuPlayerDbContext>
{
    public OsuPlayerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OsuPlayerDbContext>()
            .UseSqlite(OsuPlayerDbContext.DefaultConnectionString)
            .Options;

        return new OsuPlayerDbContext(options);
    }
}