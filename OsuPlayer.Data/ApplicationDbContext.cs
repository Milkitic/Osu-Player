using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data.Internal.Conversions;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared;

namespace Milki.OsuPlayer.Data;

public sealed partial class ApplicationDbContext : DbContext
{
#nullable disable
    public DbSet<SoftwareState> SoftwareStates { get; set; }


    public DbSet<PlayList> PlayLists { get; set; }
    public DbSet<PlayListPlayItemRelation> PlayListRelations { get; set; }

    public DbSet<PlayItem> PlayItems { get; set; }
    public DbSet<PlayItemDetail> PlayItemDetails { get; set; }
    public DbSet<PlayItemConfig> PlayItemConfigs { get; set; }
    public DbSet<PlayItemAsset> PlayItemAssets { get; set; }

    public DbSet<LoosePlayItem> CurrentPlay { get; set; }
    public DbSet<LoosePlayItem> RecentPlay { get; set; }

    public DbSet<ExportItem> Exports { get; set; }

#nullable restore

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;
        optionsBuilder.EnableSensitiveDataLogging();
        var dataBases = Path.Combine(Constants.ApplicationDir, "databases");
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayList>().HasData(new PlayList
        {
            Id = 1,
            IsDefault = true,
            Name = "Favorite"
        });
        modelBuilder.Entity<PlayItem>()
            .HasMany(p => p.PlayLists)
            .WithMany(p => p.PlayItems)
            .UsingEntity<PlayListPlayItemRelation>(
                j => j
                    .HasOne(pt => pt.PlayList)
                    .WithMany(t => t.PlayListRelations)
                    .HasForeignKey(pt => pt.PlayListId),
                j => j
                    .HasOne(pt => pt.PlayItem)
                    .WithMany(p => p.PlayListRelations)
                    .HasForeignKey(pt => pt.PlayItemId),
                j => { j.HasKey(t => new { t.PlayItemId, t.PlayListId }); });
    }

    public override int SaveChanges()
    {
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.Entity is IAutoCreatable creatable && e.State == EntityState.Added)
            {
                creatable.CreateTime = DateTime.Now;
            }
            else if (e.Entity is IAutoUpdatable updatable && e.State == EntityState.Modified)
            {
                updatable.UpdatedTime = DateTime.Now;
            }
        }

        return base.SaveChanges();
    }
}