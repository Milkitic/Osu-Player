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

        public async Task<BeatmapConfig> GetOrAddBeatmapConfig(Beatmap beatmap)
        {
            try
            {
                if (beatmap == null)
                {
                    Logger.Debug("需确认加入自定义目录后才可继续");
                    return null;
                }

                var map = await BeatmapConfigs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);

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

        public async Task DeleteCollection(Collection collection)
        {
            Relations.RemoveRange(Relations.Where(k => k.CollectionId == collection.Id));
            Collections.Remove(collection);
            await SaveChangesAsync();
        }

        public async Task DeleteBeatmapFromCollection(Beatmap beatmap, Collection collection)
        {
            if (beatmap.IsTemporary)
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
                return;
            }

            var relation = await Relations
                .FirstOrDefaultAsync(k => k.CollectionId == collection.Id && k.BeatmapId == beatmap.Id);
            Relations.Remove(relation);
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

        /// <summary>
        /// ignore null columns
        /// </summary>
        /// <param name="beatmapConfig"></param>
        /// <returns></returns>
        public async Task AddOrUpdateBeatmapConfig(BeatmapConfig beatmapConfig)
        {
            if (beatmapConfig.BeatmapId == Guid.Empty)
            {
                Console.WriteLine("No beatmap found.");
                return;
            }

            var beatmap = await Beatmaps.FindAsync(beatmapConfig.BeatmapId);
            if (beatmap == null)
            {
                Console.WriteLine("No beatmap found.");
            }

            var exist = await BeatmapConfigs.FindAsync(beatmapConfig.Id);
            if (exist == null)
                BeatmapConfigs.Add(beatmapConfig);
            else
            {
                if (beatmapConfig.MainVolume != null) exist.MainVolume = beatmapConfig.MainVolume.Value;
                if (beatmapConfig.MusicVolume != null) exist.MusicVolume = beatmapConfig.MusicVolume.Value;
                if (beatmapConfig.HitsoundVolume != null) exist.HitsoundVolume = beatmapConfig.HitsoundVolume.Value;
                if (beatmapConfig.SampleVolume != null) exist.SampleVolume = beatmapConfig.SampleVolume.Value;
                if (beatmapConfig.Offset != null) exist.Offset = beatmapConfig.Offset.Value;
                if (beatmapConfig.PlaybackRate != null) exist.PlaybackRate = beatmapConfig.PlaybackRate.Value;
                if (beatmapConfig.PlayUseTempo != null) exist.PlayUseTempo = beatmapConfig.PlayUseTempo.Value;
                if (beatmapConfig.LyricOffset != null) exist.LyricOffset = beatmapConfig.LyricOffset.Value;
                if (beatmapConfig.ForceLyricId != null) exist.ForceLyricId = beatmapConfig.ForceLyricId;
            }

            await SaveChangesAsync();
        }

        public async Task AddOrUpdateBeatmapToRecent(Beatmap beatmap)
        {
            var recent = await RecentList.FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);
            if (recent != null)
                recent.UpdateTime = DateTime.Now;
            else
                RecentList.Add(new BeatmapRecentPlay { Beatmap = beatmap, Id = Guid.NewGuid() });
            await SaveChangesAsync();
        }

        public async Task RemoveBeatmapFromRecent(Beatmap beatmap)
        {
            var recent = await RecentList.FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);
            if (recent != null)
                RecentList.Remove(recent);

            await SaveChangesAsync();
        }

        public async Task RemoveBeatmapsFromRecent(IEnumerable<Beatmap> beatmap)
        {
            var recent = RecentList.Join(beatmap, k => k.BeatmapId, k => k.Id, (k, x) => k);
            RecentList.RemoveRange(recent);

            await SaveChangesAsync();
        }

        public async Task ClearRecent()
        {
            RecentList.RemoveRange(RecentList);
            await SaveChangesAsync();
        }

        public async Task AddOrUpdateExportByBeatmap(BeatmapExport export)
        {
            if (export.BeatmapId == Guid.Empty)
            {
                Console.WriteLine("No beatmap found.");
                return;
            }

            var beatmap = await Beatmaps.FindAsync(export.BeatmapId);
            if (beatmap == null)
            {
                Console.WriteLine("No beatmap found.");
            }

            var exist = await Exports.FindAsync(export.Id);
            if (exist != null)
            {
                exist.UpdateTime = DateTime.Now;
                exist.ExportPath = export.ExportPath;
                exist.IsValid = true;
            }
            else
            {
                export.Beatmap = beatmap;
                Exports.Add(export);
            }

            await SaveChangesAsync();
        }

        public async Task RemoveExport(BeatmapExport export)
        {
            Exports.Remove(export);
            await SaveChangesAsync();
        }

        public async Task<BeatmapThumb> GetThumb(Beatmap beatmap)
        {
            var thumb = await Thumbs
                .AsNoTracking()
                .Include(k => k.BeatmapStoryboard)
                .FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);
            return thumb;
        }

        public async Task AddOrUpdateThumbPath(Beatmap beatmap, string path)
        {
            var thumb = await Thumbs.FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);
            if (thumb != null)
                thumb.ThumbPath = path;
            else
                Thumbs.Add(new BeatmapThumb { Beatmap = beatmap, ThumbPath = path, Id = Guid.NewGuid() });

            await SaveChangesAsync();
        }

        public async Task AddOrUpdateStoryboardByBeatmap(BeatmapStoryboard storyboard)
        {
            if (storyboard.BeatmapId == Guid.Empty)
            {
                Console.WriteLine("No beatmap found.");
                return;
            }

            var beatmap = await Beatmaps.FindAsync(storyboard.BeatmapId);
            if (beatmap == null)
            {
                Console.WriteLine("No beatmap found.");
            }

            var exist = await Storyboards.FindAsync(storyboard.Id);
            if (exist != null)
            {
                exist.StoryboardVideoPath = storyboard.StoryboardVideoPath;
            }
            else
            {
                storyboard.Beatmap = beatmap;
                Storyboards.Add(storyboard);
            }

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
