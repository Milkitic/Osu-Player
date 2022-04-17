using System.Drawing;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Conversions;
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
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Milki.OsuPlayer");
        var dataBases = Path.Combine(dir, "databases");
        if (!Directory.Exists(dataBases))
        {
            Directory.CreateDirectory(dataBases);
        }

        var db = Path.Combine(dataBases, "application.db");
        optionsBuilder.UseSqlite("data source=" + db);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Point?>()
            .HaveConversion<PointDbConverter>();
        configurationBuilder
            .Properties<Rectangle?>()
            .HaveConversion<RectangleDbConverter>();
        configurationBuilder
            .Properties<TimeSpan?>()
            .HaveConversion<TimespanDbConverter>();
        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<DateTimeDbConverter>();
        configurationBuilder
            .Properties<Point>()
            .HaveConversion<PointDbConverter>();
        configurationBuilder
            .Properties<Rectangle>()
            .HaveConversion<RectangleDbConverter>();
        configurationBuilder
            .Properties<TimeSpan>()
            .HaveConversion<TimespanDbConverter>();
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateTimeDbConverter>();
    }
}