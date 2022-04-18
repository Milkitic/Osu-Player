using System.Drawing;
using System.Linq.Expressions;
using Anotar.NLog;
using Microsoft.EntityFrameworkCore;
using OsuPlayer.Data.Conversions;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Data;

public sealed class ApplicationDbContext : DbContext
{
#nullable disable
    public DbSet<SoftwareState> SoftwareStates { get; set; }


    public DbSet<PlayList> PlayLists { get; set; }
    public DbSet<PlayListPlayItemRelation> PlayListRelations { get; set; }

    public DbSet<PlayItem> PlayItems { get; set; }
    public DbSet<PlayItemDetail> PlayItemDetails { get; set; }
    public DbSet<PlayItemConfig> PlayItemConfigs { get; set; }
    public DbSet<PlayItemAsset> PlayItemAssets { get; set; }

    public DbSet<LoosePlayItem> CurrentPlaying { get; set; }

    public DbSet<ExportItem> ExportList { get; set; }

#nullable restore

    public async Task<PaginationQueryResult<PlayGroupQuery>> SearchBeatmapAutoAsync(string searchText,
        BeatmapOrderOptions beatmapOrderOptions,
        int page,
        int countPerPage)
    {
        if (page <= 0) page = 1;
        try
        {
            var query = PlayItems
                .AsNoTracking()
                .Include(k => k.PlayItemAsset)
                .Join(PlayItemDetails.Where(GetWhereExpression(searchText)),
                    playItem => playItem.PlayItemDetailId,
                    playItemDetail => playItemDetail.Id,
                    (playItem, playItemDetail) => new
                    {
                        PlayItem = playItem,
                        PlayItemDetail = playItemDetail,
                        PlayItemAssets = playItem.PlayItemAsset
                    })
                .Select(k => new PlayGroupQuery
                {
                    Folder = k.PlayItem.Folder,
                    IsAutoManaged = k.PlayItem.IsAutoManaged,
                    Artist = k.PlayItemDetail.Artist,
                    ArtistUnicode = k.PlayItemDetail.ArtistUnicode,
                    Title = k.PlayItemDetail.Title,
                    TitleUnicode = k.PlayItemDetail.TitleUnicode,
                    Tags = k.PlayItemDetail.Tags,
                    Source = k.PlayItemDetail.Source,
                    Creator = k.PlayItemDetail.Creator,
                    BeatmapSetId = k.PlayItemDetail.BeatmapSetId,
                    ThumbPath = k.PlayItemAssets == null ? null : k.PlayItemAssets.ThumbPath,
                    StoryboardVideoPath = k.PlayItemAssets == null ? null : k.PlayItemAssets.StoryboardVideoPath,
                    VideoPath = k.PlayItemAssets == null ? null : k.PlayItemAssets.VideoPath,
                });

            var sqlStr = query.ToQueryString();
            var fullResult = await query.ToArrayAsync();

            var enumerable = fullResult
                .GroupBy(k => k.Folder, StringComparer.Ordinal)
                .SelectMany(k => k
                    .GroupBy(o => o, MetaComparer.Instance)
                    .Select(o => o.First())
                );

            enumerable = beatmapOrderOptions switch
            {
                BeatmapOrderOptions.Artist => enumerable.OrderBy(k =>
                        string.IsNullOrEmpty(k.ArtistUnicode) ? k.Artist : k.ArtistUnicode,
                    StringComparer.InvariantCultureIgnoreCase),
                BeatmapOrderOptions.Title => enumerable.OrderBy(k =>
                        string.IsNullOrEmpty(k.TitleUnicode) ? k.Title : k.TitleUnicode,
                    StringComparer.InvariantCultureIgnoreCase),
                BeatmapOrderOptions.Creator => enumerable.OrderBy(k => k.Creator,
                    StringComparer.OrdinalIgnoreCase),
                _ => throw new ArgumentOutOfRangeException(nameof(beatmapOrderOptions), beatmapOrderOptions, null)
            };

            var bufferResult = enumerable.ToArray();
            var totalCount = bufferResult.Length;
            var beatmaps = bufferResult.Skip((page - 1) * countPerPage).Take(countPerPage).ToArray();

            return new PaginationQueryResult<PlayGroupQuery>(beatmaps, totalCount);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while searching beatmap.", ex);
            throw;
        }
    }

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
                j =>
                {
                    j.HasKey(t => new { t.PlayItemId, t.PlayListId });
                });
    }

    private static Expression<Func<PlayItemDetail, bool>> GetWhereExpression(string searchText)
    {
        return k =>
            k.Artist.Contains(searchText) ||
            k.ArtistUnicode.Contains(searchText) ||
            k.Title.Contains(searchText) ||
            k.TitleUnicode.Contains(searchText) ||
            k.Tags.Contains(searchText) ||
            k.Source.Contains(searchText) ||
            k.Creator.Contains(searchText) ||
            k.Version.Contains(searchText);
    }
}