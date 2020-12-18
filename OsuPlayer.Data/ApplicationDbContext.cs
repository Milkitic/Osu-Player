using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collection = Milky.OsuPlayer.Data.Models.Collection;

namespace Milky.OsuPlayer.Data
{
    public class ApplicationDbContext : DbContextBase
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
            modelBuilder.Entity<Collection>().HasData(new Collection
            {
                IsDefault = true,
                Name = "Favorite"
            });
        }

        #region Beatmap

        public async Task<PaginationQueryResult<Beatmap>> SearchBeatmapByOptions(
            string searchText,
            BeatmapOrderOptions beatmapOrderOptions,
            int page,
            int countPerPage)
        {
            var sqliteParameters = new List<SqliteParameter>();
            var command = " SELECT * FROM beatmap WHERE ";
            var keywordSql = GetKeywordQueryAndArgs(searchText, ref sqliteParameters);
            var sort = GetOrderAndTakeQueryAndArgs(beatmapOrderOptions, page, countPerPage);
            var sw = Stopwatch.StartNew();
            try
            {

                var totalCount = await Beatmaps.FromSqlRaw(command + keywordSql).CountAsync();
                var beatmaps = await Beatmaps
                    .FromSqlRaw(command + keywordSql + sort, sqliteParameters.Cast<object>().ToArray())
                    .ToListAsync();
                return new PaginationQueryResult<Beatmap>(beatmaps, totalCount);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling SearchBeatmapByOptions().");
                throw;
            }
            finally
            {
                Logger.Debug("查询花费: {0}", sw.ElapsedMilliseconds);
                sw.Stop();
            }
        }

        public async Task<List<Beatmap>> GetBeatmapsFromFolder(string folder, bool inOwnDb)
        {
            try
            {
                return await Beatmaps.Where(k => k.InOwnDb == inOwnDb && k.FolderNameOrPath == folder).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsFromFolder().");
                throw;
            }
        }

        // questionable
        public async Task SyncBeatmapsFromHoLLy(IEnumerable<BeatmapEntry> entries)
        {
            var all = Beatmaps;
            var allBeatmaps = entries.Select(BeatmapConvertExtension.ParseFromHolly).ToList();
            var allIds = allBeatmaps.Select(k => k.Id).ToList();

            var nonExistAnyMore = all.Where(k => !allIds.Contains(k.Id));
            Beatmaps.RemoveRange(nonExistAnyMore);

            var exists = await all.Where(k => allIds.Contains(k.Id)).Select(k => k.Id).ToListAsync();
            Beatmaps.UpdateRange(allBeatmaps.Where(k => exists.Contains(k.Id)));

            var news = allIds.Except(exists);
            Beatmaps.AddRange(allBeatmaps.Where(k => news.Contains(k.Id)));

            await SaveChangesAsync();
        }

        public async Task AddNewBeatmaps(IEnumerable<Beatmap> beatmaps)
        {
            Beatmaps.AddRange(beatmaps);
            await SaveChangesAsync();
        }

        public async Task RemoveLocalAll()
        {
            Beatmaps.RemoveRange(Beatmaps.Where(k => k.InOwnDb));
            await SaveChangesAsync();
        }

        public async Task RemoveSyncedAll()
        {
            Beatmaps.RemoveRange(Beatmaps.Where(k => !k.InOwnDb));
            await SaveChangesAsync();
        }

        #endregion

        #region BeatmapConfig

        public async Task<BeatmapConfig> GetOrAddBeatmapConfigByBeatmap(Beatmap beatmap)
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

        /// <summary>
        /// ignore null columns
        /// </summary>
        /// <param name="beatmapConfig"></param>
        /// <returns></returns>
        public async Task AddOrUpdateBeatmapConfig(BeatmapConfig beatmapConfig)
        {
            if (string.IsNullOrEmpty(beatmapConfig.BeatmapId))
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

        #endregion

        #region Collection

        public async Task<List<Collection>> GetCollections()
        {
            return await Collections
                .AsNoTracking()
                .OrderByDescending(k => k.Index)
                .ToListAsync();
        }

        public async Task<PaginationQueryResult<Beatmap>> GetBeatmapsFromCollection(Collection collection,
            int page = 0,
            int countPerPage = 50,
            BeatmapOrderOptions options = BeatmapOrderOptions.CreateTime)
        {
            if (collection.Id == Guid.Empty)
            {
                Console.WriteLine("No collection found.");
                return new PaginationQueryResult<Beatmap>(new List<Beatmap>(), 0);
            }

            collection = await Collections.FindAsync(collection.Id);
            if (collection == null)
            {
                Console.WriteLine("No collection found.");
                return new PaginationQueryResult<Beatmap>(new List<Beatmap>(), 0);
            }

            var relations = Relations
                .AsNoTracking()
                .Where(k => k.CollectionId == collection.Id);

            var beatmaps = relations.Join(Beatmaps, k => k.BeatmapId, k => k.Id, (k, x) => x);

            var count = await beatmaps.CountAsync();

            var enumerable = options switch
            {
                BeatmapOrderOptions.UpdateTime => beatmaps.OrderByDescending(k => k.UpdateTime),
                BeatmapOrderOptions.CreateTime => beatmaps.OrderByDescending(k => k.CreateTime),
                _ => throw new ArgumentOutOfRangeException(nameof(options), options, null)
            };

            var result = await enumerable
                .Skip(page * countPerPage)
                .Take(countPerPage)
                .ToListAsync();

            return new PaginationQueryResult<Beatmap>(result, count);
        }

        public async Task<List<Collection>> GetCollectionsByBeatmap(Beatmap beatmap)
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
                IsDefault = locked,
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

        public async Task DeleteBeatmapsFromCollection(IEnumerable<Beatmap> beatmaps, Collection collection)
        {
            var ids = beatmaps.Select(k => k.Id).ToList();

            var relations = Relations
                .Where(k => k.CollectionId == collection.Id && ids.Contains(k.BeatmapId));
            Relations.RemoveRange(relations);
            await SaveChangesAsync();
        }
        public async Task DeleteBeatmapFromCollection(Beatmap beatmap, Collection collection)
        {
            if (beatmap.IsTemporary)
            {
                Logger.Warn("需确认加入自定义目录后才可继续");
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

        //todo: addorupdate
        public async Task AddBeatmapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            if (beatmaps.Count < 1) return;

            var maps = beatmaps.Where(k => !k.IsTemporary);
            collection = await Collections.FindAsync(collection.Id);
            collection.Beatmaps.AddRange(maps);

            await SaveChangesAsync();
        }

        #endregion

        #region Recent

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

        #endregion
        public async Task AddOrUpdateBeatmapToPlaylist(Beatmap beatmap)
        {
            if (beatmap.IsTemporary)
                throw new Exception("The beatmap is temporary which can not be added to playlist.");
            var map = await Playlist.FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);
            if (map == null)
            {
                Playlist.Add(new BeatmapCurrentPlay
                {
                    Id = Guid.NewGuid(),
                    Beatmap = beatmap,
                    PlayTime = DateTime.Now
                });
            }
            else
            {
                map.PlayTime = DateTime.Now;
            }

            await SaveChangesAsync();
        }

        public async Task UpdateBeatmapsToPlaylist(IEnumerable<Beatmap> songList)
        {
            var requestIdDic = songList
                .Where(k => k != null)
                .Select((beatmap, index) => (index, beatmap))
                .ToDictionary(k => k.beatmap.Id, k => k.index);
            var requestId = requestIdDic.Keys.ToHashSet();

            var nonExistAnyMore = Playlist.Where(k => !requestId.Contains(k.BeatmapId));
            Playlist.RemoveRange(nonExistAnyMore);

            var exists = await Playlist.Where(k => requestId.Contains(k.BeatmapId)).ToListAsync();
            foreach (var beatmapCurrentPlay in exists)
            {
                beatmapCurrentPlay.Index = requestIdDic[beatmapCurrentPlay.BeatmapId];
            }

            Playlist.UpdateRange(exists);

            var news = Playlist.Except(exists);
            foreach (var beatmapCurrentPlay in news)
            {
                beatmapCurrentPlay.Index = requestIdDic[beatmapCurrentPlay.BeatmapId];
            }

            Playlist.AddRange(news);

            await SaveChangesAsync();
        }

        #region Export

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

        public async Task AddOrUpdateExport(BeatmapExport export)
        {
            if (string.IsNullOrEmpty(export.BeatmapId))
            {
                Console.WriteLine("No beatmap found.");
                return;
            }

            var beatmap = await Beatmaps.FindAsync(export.BeatmapId);
            if (beatmap == null)
            {
                Console.WriteLine("No beatmap found.");
                return;
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

        public async Task RemoveExports(IEnumerable<BeatmapExport> exports)
        {
            Exports.RemoveRange(exports);
            await SaveChangesAsync();
        }

        #endregion

        #region Thumb

        public async Task<BeatmapThumb> GetThumb(Beatmap beatmap)
        {
            var thumb = await Thumbs
                .AsNoTracking()
                .Include(k => k.BeatmapStoryboard)
                .FirstOrDefaultAsync(k => k.BeatmapId == beatmap.Id);
            return thumb;
        }


        public async Task<List<Beatmap>> FillBeatmapThumbs(List<Beatmap> dbMaps)
        {
            var allIds = dbMaps.Select(k => k.Id).ToList();
            var withThumbs = await Beatmaps
                .Where(k => allIds.Contains(k.Id))
                .Include(k => k.BeatmapThumb)
                .ToListAsync();
            return withThumbs;
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

        #endregion

        #region Storyboard

        public async Task AddOrUpdateStoryboardByBeatmap(BeatmapStoryboard storyboard)
        {
            if (storyboard.BeatmapId == 0)
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

        #endregion

        private static string GetKeywordQueryAndArgs(string keywordStr, ref List<SqliteParameter> sqliteParameters)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
            {
                return "1=1";
            }

            var keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            sqliteParameters ??= new List<SqliteParameter>();

            var sb = new StringBuilder();
            for (var i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i];
                var postfix = $" like @keyword{i} ";
                sb.AppendLine("(")
                    .AppendLine($" artist {postfix} OR ")
                    .AppendLine($" artistU {postfix} OR ")
                    .AppendLine($" title {postfix} OR ")
                    .AppendLine($" titleU {postfix} OR ")
                    .AppendLine($" tags {postfix} OR ")
                    .AppendLine($" source {postfix} OR ")
                    .AppendLine($" creator {postfix} OR ")
                    .AppendLine($" version {postfix} ")
                    .AppendLine(" ) ");

                sqliteParameters.Add(new SqliteParameter($"keyword{i}", $"%{keyword}%"));
                if (i != keywords.Length - 1)
                {
                    sb.AppendLine(" AND ");
                }
            }

            return sb.ToString();
        }

        private static string GetOrderAndTakeQueryAndArgs(BeatmapOrderOptions beatmapOrderOptions, int page, int countPerPage)
        {
            string orderBy = beatmapOrderOptions switch
            {
                BeatmapOrderOptions.Title => " ORDER BY titleU, title ",
                BeatmapOrderOptions.CreateTime => " ORDER BY CreateTime DESC ",
                BeatmapOrderOptions.UpdateTime => " ORDER BY UpdateTime DESC ",
                BeatmapOrderOptions.Artist => " ORDER BY artistU, artist ",
                _ => throw new ArgumentOutOfRangeException(nameof(beatmapOrderOptions), beatmapOrderOptions, null)
            };

            string limit = $" LIMIT {page * countPerPage}, {countPerPage} ";
            return orderBy + limit;
        }
    }
}
