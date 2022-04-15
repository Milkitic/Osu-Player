using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Data;

public sealed class ApplicationDbContext : DbContext
{
#nullable disable
    public DbSet<SoftwareState> SoftwareStates { get; set; }
    public DbSet<PlayItem> PlayItems { get; set; }
    public DbSet<PlayItemDetail> PlayItemDetails { get; set; }
#nullable restore

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        optionsBuilder.EnableSensitiveDataLogging();
        if (!Directory.Exists("./databases"))
        {
            Directory.CreateDirectory("./databases");
        }

        optionsBuilder.UseSqlite("data source=./databases/application.db");
    }
}