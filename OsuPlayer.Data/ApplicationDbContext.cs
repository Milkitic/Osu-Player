using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data.Models;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Data
{
    public class ApplicationDbContext : DbContext
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public DbSet<Beatmap> Beatmaps { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionRelation> Relations { get; set; }
        public DbSet<BeatmapConfig> BeatmapConfigs { get; set; }
        public DbSet<BeatmapCurrentPlay> Playlist { get; set; }
        public DbSet<BeatmapRecentPlay> RecentList { get; set; }
        public DbSet<BeatmapExport> Exports { get; set; }
        public DbSet<BeatmapThumb> Thumbs { get; set; }
        public DbSet<BeatmapStoryboard> Storyboards { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("data source=player.db", options => { });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public async Task<BeatmapConfig> GetOrAddBeatmapConfig(Guid id)
        {
            try
            {
                var beatmap = await Beatmaps.FindAsync(id);
                if (beatmap == null)
                {
                    Logger.Debug("需确认加入自定义目录后才可继续");
                    return null;
                }

                var map = await BeatmapConfigs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.BeatmapId == id);

                if (map != null) return map;

                var guid = Guid.NewGuid();
                BeatmapConfigs.Add(new BeatmapConfig()
                {
                    Id = guid,
                    BeatmapId = beatmap.Id,

                });
                await SaveChangesAsync();

                return await BeatmapConfigs.FindAsync(guid);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public async Task<PaginationQueryResult<Beatmap>> GetRecentList(
            int page = 0,
            int countPerPage = 50,
            BeatmapOrderOptions options = BeatmapOrderOptions.UpdateTime)
        {
            var list = RecentList.AsNoTracking();
            var queryable = options switch
            {
                BeatmapOrderOptions.UpdateTime => list.OrderByDescending(k => k.UpdateTime),
                _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
            };

            var count = await queryable.CountAsync();
            var collection = queryable
                .Include(k => k.Beatmap);

            var result = await collection
                .Skip(page * countPerPage)
                .Take(countPerPage)
                .Select(k => k.Beatmap)
                .ToListAsync();

            return new PaginationQueryResult<Beatmap>(result, count);
        }

        public async Task<PaginationQueryResult<BeatmapExport>> GetExportList(
            int page = 0,
            int countPerPage = 50,
            BeatmapOrderOptions options = BeatmapOrderOptions.UpdateTime)
        {
            var list = Exports.AsNoTracking();
            var queryable = options switch
            {
                BeatmapOrderOptions.UpdateTime => list.OrderByDescending(k => k.CreateTime),
                _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
            };

            var count = await queryable.CountAsync();
            var collection = queryable
                .Include(k => k.Beatmap);

            var result = await collection
                .Skip(page * countPerPage)
                .Take(countPerPage)
                .ToListAsync();

            return new PaginationQueryResult<BeatmapExport>(result, count);
        }

        public async Task<List<Collection>> GetCollections()
        {
            return await Collections
                .AsNoTracking()
                .OrderByDescending(k => k.Index)
                .ToListAsync();
        }

        public async Task<List<Beatmap>> GetBeatmapFromCollection(Collection collection)
        {
            collection = await Collections
                .AsNoTracking()
                .Include(k => k.Beatmaps)
                .FirstOrDefaultAsync(k => k.Id == collection.Id);

            return collection.Beatmaps;
        }

        public async Task<List<Collection>> GetCollectionsByMap(Beatmap beatmap)
        {
            beatmap = await Beatmaps
                .AsNoTracking()
                .Include(k => k.Collections)
                .FirstOrDefaultAsync(k => k.Id == beatmap.Id);
            return beatmap.Collections;
        }

        public async Task AddOrUpdateCollection(Collection collection)
        {
            var result = await GetCollection(collection.Id);
            if (result == null)
            {
                await AddCollection(collection.Name);
            }
            else
            {
                Collections.Update(collection);
                await SaveChangesAsync();
            }
        }

        public async Task AddCollection(string name, bool locked = false)
        {
            var count = await Collections.CountAsync();
            var maxIndex = count > 0 ? await Collections.MaxAsync(k => k.Index) : -1;
            var collection = new Collection
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsLocked = locked,
                Index = maxIndex + 1
            };

            Collections.Add(collection);
            await SaveChangesAsync();
        }

        public async Task<Collection> GetCollection(Guid id)
        {
            return await Collections.FindAsync(id);
        }

        public async Task AddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            if (beatmaps.Count < 1) return;

            var maps = beatmaps.Where(k => !k.IsMapTemporary);
            collection = await Collections.FindAsync(collection.Id);
            collection.Beatmaps.AddRange(maps);

            await SaveChangesAsync();
        }
    }

    public enum BeatmapOrderOptions
    {
        UpdateTime
    }

    public class PaginationQueryResult<T> where T : class
    {
        public PaginationQueryResult(List<T> collection, int count)
        {
            Collection = collection;
            Count = count;
        }

        public List<T> Collection { get; set; }
        public int Count { get; set; }
    }
}
