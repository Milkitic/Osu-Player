using System.Diagnostics;
using System.Drawing;
using System.Text;
using Anotar.NLog;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using OsuPlayer.Data.Conversions;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Data;

public enum BeatmapOrderOptions
{
    ArtistUnicode,
    TitleUnicode,
    //CreateTime,
    //UpdateTime,
    Creator,
    Index,
}

public sealed class ApplicationDbContext : DbContext
{
#nullable disable
    public DbSet<SoftwareState> SoftwareStates { get; set; }
    public DbSet<PlayItem> PlayItems { get; set; }
    public DbSet<PlayItemDetail> PlayItemDetails { get; set; }
#nullable restore

    private const string SearchCommand1 =
        @"SELECT d.Id, p.Path, p.IsAutoManaged, p.PlayItemDetailId FROM (SELECT * FROM PlayItemDetails WHERE ";

    private const string SearchCommand2 = @") d INNER JOIN PlayItems p ON p.PlayItemDetailId = d.Id";

    public async Task<PaginationQueryResult<PlayItem>> SearchBeatmapAsyncOld(string searchText,
        BeatmapOrderOptions beatmapOrderOptions,
        int page,
        int countPerPage)
    {
        var sqliteParameters = new List<SqliteParameter>();
        var keywordSql = GetKeywordQueryAndArgs(searchText, ref sqliteParameters);

        var sort = GetOrderAndTakeQueryAndArgs(beatmapOrderOptions, page, countPerPage);
        try
        {
            var sql = SearchCommand1 + keywordSql + SearchCommand2;
            var totalCount = await PlayItems
                .FromSqlRaw(sql, sqliteParameters.Cast<object>().ToArray())
                .AsNoTracking()
                .CountAsync();

            var s = sql + sort;
            var beatmaps = await PlayItems
                .FromSqlRaw(s, sqliteParameters.Cast<object>().ToArray())
                .AsNoTracking()
                .Include(k => k.PlayItemDetail)
                .ToArrayAsync();
            return new PaginationQueryResult<PlayItem>(beatmaps, totalCount);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while searching beatmap.", ex);
            throw;
        }
    }

    public async Task<PaginationQueryResult<PlayItemQuery>> SearchBeatmapAsync(string searchText,
        BeatmapOrderOptions beatmapOrderOptions,
        int page,
        int countPerPage)
    {
        if (page <= 0) page = 1;
        try
        {
            var query = PlayItems.AsNoTracking().Join(
                PlayItemDetails.Where(k =>
                    k.Artist.Contains(searchText) ||
                    k.ArtistUnicode.Contains(searchText) ||
                    k.Title.Contains(searchText) ||
                    k.TitleUnicode.Contains(searchText) ||
                    k.Tags.Contains(searchText) ||
                    k.Source.Contains(searchText) ||
                    k.Creator.Contains(searchText) ||
                    k.Version.Contains(searchText)),
                playItem => playItem.PlayItemDetailId,
                playItemDetail => playItemDetail.Id,
                (playItem, playItemDetail) => new PlayItemQuery
                {
                    Id = playItem.Id,
                    Path = playItem.Path,
                    IsAutoManaged = playItem.IsAutoManaged,
                    Artist = playItemDetail.Artist,
                    ArtistUnicode = playItemDetail.ArtistUnicode,
                    Title = playItemDetail.Title,
                    TitleUnicode = playItemDetail.TitleUnicode,
                    Tags = playItemDetail.Tags,
                    Source = playItemDetail.Source,
                    Creator = playItemDetail.Creator,
                    Version = playItemDetail.Version,
                    DefaultStarRatingStd = playItemDetail.DefaultStarRatingStd,
                    DefaultStarRatingTaiko = playItemDetail.DefaultStarRatingTaiko,
                    DefaultStarRatingCtB = playItemDetail.DefaultStarRatingCtB,
                    DefaultStarRatingMania = playItemDetail.DefaultStarRatingMania,
                    BeatmapId = playItemDetail.BeatmapId,
                    BeatmapSetId = playItemDetail.BeatmapSetId,
                    GameMode = playItemDetail.GameMode
                });

            var totalCount = await query.CountAsync();
            query = beatmapOrderOptions switch
            {
                BeatmapOrderOptions.Index => query.OrderBy(k => k.Id),
                BeatmapOrderOptions.ArtistUnicode => query.OrderBy(k => k.ArtistUnicode),
                BeatmapOrderOptions.TitleUnicode => query.OrderBy(k => k.TitleUnicode),
                BeatmapOrderOptions.Creator => query.OrderBy(k => k.Creator),
                _ => throw new ArgumentOutOfRangeException(nameof(beatmapOrderOptions), beatmapOrderOptions, null)
            };
            query = query.Skip((page - 1) * countPerPage).Take(countPerPage);
            var beatmaps = await query.ToListAsync();

            return new PaginationQueryResult<PlayItemQuery>(beatmaps, totalCount);
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

    private static string GetKeywordQueryAndArgs(string keywordStr, ref List<SqliteParameter> sqliteParameters)
    {
        if (string.IsNullOrWhiteSpace(keywordStr))
        {
            return "1=1";
        }

        var keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        for (var i = 0; i < keywords.Length; i++)
        {
            var keyword = keywords[i];
            var postfix = $" like @keyword{i} ";
            sb.AppendLine("(")
                .AppendLine($" Artist {postfix} OR ")
                .AppendLine($" ArtistUnicode {postfix} OR ")
                .AppendLine($" Title {postfix} OR ")
                .AppendLine($" TitleUnicode {postfix} OR ")
                .AppendLine($" Tags {postfix} OR ")
                .AppendLine($" Source {postfix} OR ")
                .AppendLine($" Creator {postfix} OR ")
                .AppendLine($" Version {postfix} ")
                .AppendLine(" ) ");

            sqliteParameters.Add(new SqliteParameter($"keyword{i}", $"%{keyword}%"));
            if (i != keywords.Length - 1)
            {
                sb.AppendLine(" AND ");
            }
        }

        return sb.ToString();
    }

    private static string GetOrderAndTakeQueryAndArgs(BeatmapOrderOptions beatmapOrderOptions, int page,
        int countPerPage)
    {
        string orderBy = beatmapOrderOptions switch
        {
            BeatmapOrderOptions.Index => " ORDER BY p.Id ",
            BeatmapOrderOptions.TitleUnicode => " ORDER BY d.TitleUnicode, d.Title ",
            //BeatmapOrderOptions.UpdateTime => " ORDER BY d.UpdateTime DESC ",
            BeatmapOrderOptions.ArtistUnicode => " ORDER BY d.ArtistUnicode, d.Artist ",
            BeatmapOrderOptions.Creator => " ORDER BY d.Creator ",
            _ => throw new ArgumentOutOfRangeException(nameof(beatmapOrderOptions), beatmapOrderOptions, null)
        };

        string limit = $" LIMIT {page * countPerPage}, {countPerPage} ";
        return orderBy + limit;
    }
}