using System.Drawing;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Conversions;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Data;

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

    public async Task<PlayItem> GetOrAddPlayItem(string standardizedPath)
    {
        var playItem = await PlayItems
            .AsNoTracking()
            .Include(k => k.PlayItemDetail)
            .Include(k => k.PlayItemConfig)
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayLists)
            .Include(k => k.PlayListRelations)
            .FirstOrDefaultAsync(k => k.Path == standardizedPath);

        if (playItem != null)
        {
            bool changed = false;
            if (playItem.PlayItemConfig == null)
            {
                playItem.PlayItemConfig = new PlayItemConfig();
                changed = true;
            }

            if (playItem.PlayItemAsset == null)
            {
                playItem.PlayItemAsset = new PlayItemAsset();
                changed = true;
            }

            if (changed)
            {
                await SaveChangesAsync();
            }

            Entry(playItem).State = EntityState.Detached;
            return playItem;
        }

        var folder = PathUtils.GetFolder(standardizedPath);
        var entity = new PlayItem
        {
            Path = standardizedPath,
            IsAutoManaged = false,
            Folder = folder,
            PlayItemAsset = new PlayItemAsset(),
            PlayItemConfig = new PlayItemConfig(),
            PlayItemDetail = new PlayItemDetail()
            {
                Artist = "",
                ArtistUnicode = "",
                Title = "",
                TitleUnicode = "",
                Creator = "",
                Version = "",
                BeatmapFileName = "",
                Source = "",
                Tags = "",
                FolderName = folder,
                AudioFileName = ""
            },
        };

        PlayItems.Add(entity);
        await SaveChangesAsync();
        return entity;
    }

    public async Task<PlayItem> GetPlayItemByDetail(PlayItemDetail playItemDetail, bool createExtraInfos)
    {
        if (!createExtraInfos)
        {
            return await PlayItems
                .AsNoTracking()
                .Include(k => k.PlayItemDetail)
                .Include(k => k.PlayItemConfig)
                .Include(k => k.PlayItemAsset)
                .Include(k => k.PlayLists)
                .Include(k => k.PlayListRelations)
                .FirstAsync(k => k.PlayItemDetailId == playItemDetail.Id);
        }

        var playItem = await PlayItems
            .Include(k => k.PlayItemDetail)
            .Include(k => k.PlayItemConfig)
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayLists)
            .Include(k => k.PlayListRelations)
            .FirstAsync(k => k.PlayItemDetailId == playItemDetail.Id);


        bool changed = false;
        if (playItem.PlayItemConfig == null)
        {
            playItem.PlayItemConfig = new PlayItemConfig();
            changed = true;
        }

        if (playItem.PlayItemAsset == null)
        {
            playItem.PlayItemAsset = new PlayItemAsset();
            changed = true;
        }

        if (changed)
        {
            await SaveChangesAsync();
        }

        Entry(playItem).State = EntityState.Detached;
        return playItem;
    }

    public async Task<IReadOnlyList<PlayItemDetail>> GetPlayItemDetailsByFolderAsync(string standardizedFolder)
    {
        return await PlayItemDetails
            .AsNoTracking()
            .Where(k => k.FolderName == standardizedFolder)
            .ToArrayAsync();
    }

    public async Task UpdateThumbPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.Path);
        item.PlayItemAsset!.ThumbPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

    public async Task UpdateVideoPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.Path);
        item.PlayItemAsset!.VideoPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

    public async Task UpdateStoryboardVideoPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.Path);
        item.PlayItemAsset!.StoryboardVideoPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

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