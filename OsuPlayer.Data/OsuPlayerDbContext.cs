using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Data
{
    public class OsuPlayerDbContext : DbContext
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private const int MaxIdentitiesPerQuery = 300;

        public static string DefaultDatabasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.db");
        public static string LegacyDatabasePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "player.db");
        public static string DefaultConnectionString => $"Data Source={DefaultDatabasePath}";

        static OsuPlayerDbContext()
        {
            ConfigureDapperCompatibility();
        }

        public OsuPlayerDbContext()
        {
        }

        public OsuPlayerDbContext(DbContextOptions<OsuPlayerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Beatmap> Beatmaps { get; set; }
        public DbSet<BeatmapSettings> BeatmapSettings { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionRelation> CollectionRelations { get; set; }
        public DbSet<MapThumb> MapThumbs { get; set; }
        public DbSet<StoryboardInfo> StoryboardInfos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(DefaultConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureBeatmap(modelBuilder);
            ConfigureBeatmapSettings(modelBuilder);
            ConfigureCollection(modelBuilder);
            ConfigureCollectionRelation(modelBuilder);
            ConfigureMapThumb(modelBuilder);
            ConfigureStoryboardInfo(modelBuilder);
        }

        private static void ConfigureBeatmap(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Beatmap>();

            entity.ToTable("beatmaps");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => new { k.FolderName, k.Version, k.InOwnDb })
                .IsUnique()
                .HasDatabaseName("ux_beatmaps_identity");
            entity.HasIndex(k => k.FolderName).HasDatabaseName("ix_beatmaps_folder_name");
            entity.HasIndex(k => k.BeatmapSetId).HasDatabaseName("ix_beatmaps_osu_beatmapset_id");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Artist).HasColumnName("artist");
            entity.Property(k => k.ArtistUnicode).HasColumnName("artist_unicode");
            entity.Property(k => k.Title).HasColumnName("title");
            entity.Property(k => k.TitleUnicode).HasColumnName("title_unicode");
            entity.Property(k => k.Creator).HasColumnName("creator");
            entity.Property(k => k.Version).HasColumnName("difficulty_name");
            entity.Property(k => k.BeatmapFileName).HasColumnName("beatmap_file_name");
            entity.Property(k => k.LastModifiedTime).HasColumnName("last_modified_at");
            entity.Property(k => k.DiffSrNoneStandard).HasColumnName("star_rating_standard");
            entity.Property(k => k.DiffSrNoneTaiko).HasColumnName("star_rating_taiko");
            entity.Property(k => k.DiffSrNoneCtB).HasColumnName("star_rating_catch");
            entity.Property(k => k.DiffSrNoneMania).HasColumnName("star_rating_mania");
            entity.Property(k => k.DrainTimeSeconds).HasColumnName("drain_time_seconds");
            entity.Property(k => k.TotalTime).HasColumnName("total_time_ms");
            entity.Property(k => k.AudioPreviewTime).HasColumnName("preview_time_ms");
            entity.Property(k => k.BeatmapId).HasColumnName("osu_beatmap_id");
            entity.Property(k => k.BeatmapSetId).HasColumnName("osu_beatmapset_id");
            entity.Property(k => k.GameMode).HasColumnName("game_mode");
            entity.Property(k => k.SongSource).HasColumnName("source");
            entity.Property(k => k.SongTags).HasColumnName("tags");
            entity.Property(k => k.FolderName).HasColumnName("folder_name");
            entity.Property(k => k.AudioFileName).HasColumnName("audio_file_name");
            entity.Property(k => k.InOwnDb).HasColumnName("is_local");

            entity.Ignore(k => k.StarRatingStd);
            entity.Ignore(k => k.StarRatingTaiko);
            entity.Ignore(k => k.StarRatingCtb);
            entity.Ignore(k => k.StarRatingMania);
            entity.Ignore(k => k.AutoTitle);
            entity.Ignore(k => k.AutoArtist);
        }

        private static void ConfigureBeatmapSettings(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<BeatmapSettings>();

            entity.ToTable("beatmap_play_settings");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => new { k.FolderName, k.Version, k.InOwnDb })
                .IsUnique()
                .HasDatabaseName("ux_beatmap_play_settings_identity");
            entity.HasIndex(k => k.LastPlayTime).HasDatabaseName("ix_beatmap_play_settings_last_played_at");
            entity.HasIndex(k => k.ExportFile).HasDatabaseName("ix_beatmap_play_settings_exported_file_path");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Version).HasColumnName("difficulty_name").IsRequired();
            entity.Property(k => k.FolderName).HasColumnName("folder_name").IsRequired();
            entity.Property(k => k.InOwnDb).HasColumnName("is_local");
            entity.Property(k => k.Offset).HasColumnName("audio_offset_ms");
            entity.Property(k => k.LastPlayTime).HasColumnName("last_played_at");
            entity.Property(k => k.ExportFile).HasColumnName("exported_file_path");

            entity.Ignore(k => k.AddTime);
        }

        private static void ConfigureCollection(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Collection>();

            entity.ToTable("collections");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.Name).HasDatabaseName("ix_collections_name");
            entity.HasIndex(k => k.Index).HasDatabaseName("ix_collections_sort_order");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(k => k.Locked).HasColumnName("is_locked");
            entity.Property(k => k.Index).HasColumnName("sort_order");
            entity.Property(k => k.ImagePath).HasColumnName("cover_image_path").HasMaxLength(700);
            entity.Property(k => k.Description).HasColumnName("description").HasMaxLength(700);
            entity.Property(k => k.CreateTime).HasColumnName("created_at");

            entity.Ignore(k => k.LockedBool);
        }

        private static void ConfigureCollectionRelation(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<CollectionRelation>();

            entity.ToTable("collection_beatmaps");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => new { k.CollectionId, k.MapId })
                .IsUnique()
                .HasDatabaseName("ux_collection_beatmaps_collection_map");
            entity.HasIndex(k => k.MapId).HasDatabaseName("ix_collection_beatmaps_beatmap_settings_id");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.CollectionId).HasColumnName("collection_id").IsRequired();
            entity.Property(k => k.MapId).HasColumnName("beatmap_settings_id").IsRequired();
            entity.Property(k => k.AddTime).HasColumnName("added_at");

            entity.HasOne<Collection>()
                .WithMany()
                .HasForeignKey(k => k.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<BeatmapSettings>()
                .WithMany()
                .HasForeignKey(k => k.MapId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureMapThumb(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MapThumb>();

            entity.ToTable("beatmap_thumbnails");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.MapId)
                .IsUnique()
                .HasDatabaseName("ux_beatmap_thumbnails_beatmap_id");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.MapId).HasColumnName("beatmap_id");
            entity.Property(k => k.ThumbPath).HasColumnName("thumbnail_path");
        }

        private static void ConfigureStoryboardInfo(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<StoryboardInfo>();

            entity.ToTable("storyboard_assets");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => k.MapId)
                .IsUnique()
                .HasDatabaseName("ux_storyboard_assets_beatmap_id");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.MapId).HasColumnName("beatmap_id").IsRequired();
            entity.Property(k => k.SbThumbPath).HasColumnName("thumbnail_path").IsRequired();
            entity.Property(k => k.SbThumbVideoPath).HasColumnName("preview_video_path").IsRequired();
            entity.Property(k => k.Version).HasColumnName("difficulty_name").IsRequired();
            entity.Property(k => k.FolderName).HasColumnName("folder_name").IsRequired();
            entity.Property(k => k.InOwnDb).HasColumnName("is_local");
        }

        public static async Task InitializeDatabaseAsync()
        {
            try
            {
                await using var db = new OsuPlayerDbContext();
                await db.Database.MigrateAsync();
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
                await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000");
                LegacyPlayerDatabaseMigrator.MigrateIfRequired(DefaultDatabasePath, LegacyDatabasePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while initializing local database.");
                throw;
            }
        }

        public static async Task ValidateDbAsync()
        {
            await InitializeDatabaseAsync();
        }

        public async Task<BeatmapSettings> GetMapFromDbAsync(IMapIdentifiable id)
        {
            try
            {
                LogTemporaryMap(id);

                var map = await BeatmapSettings.FirstOrDefaultAsync(k =>
                    k.Version == id.Version &&
                    k.FolderName == id.FolderName &&
                    k.InOwnDb == id.InOwnDb);

                if (map != null)
                {
                    return map;
                }

                map = new BeatmapSettings
                {
                    Id = Guid.NewGuid().ToString(),
                    Version = id.Version,
                    FolderName = id.FolderName,
                    InOwnDb = id.InOwnDb,
                    Offset = 0
                };

                BeatmapSettings.Add(map);
                await SaveChangesAsync();
                return map;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetMapFromDbAsync().");
                throw;
            }
        }

        public async Task<List<BeatmapSettings>> GetRecentListAsync()
        {
            return await BeatmapSettings
                .Where(k => k.LastPlayTime != null)
                .OrderBy(k => k.LastPlayTime)
                .ToListAsync();
        }

        public async Task<List<BeatmapSettings>> GetExportedMapsAsync()
        {
            return await BeatmapSettings
                .Where(k => k.ExportFile != null && k.ExportFile != "")
                .ToListAsync();
        }

        public async Task<List<BeatmapSettings>> GetMapsFromCollectionAsync(Collection collection)
        {
            return await CollectionRelations
                .Where(relation => relation.CollectionId == collection.Id)
                .Join(BeatmapSettings,
                    relation => relation.MapId,
                    map => map.Id,
                    (relation, map) => new BeatmapSettings(
                        map.Id,
                        map.Version,
                        map.FolderName,
                        map.Offset,
                        map.LastPlayTime,
                        map.ExportFile,
                        relation.AddTime)
                    {
                        InOwnDb = map.InOwnDb
                    })
                .ToListAsync();
        }

        public async Task<List<Collection>> GetCollectionsAsync()
        {
            return await Collections.ToListAsync();
        }

        public async Task<List<Collection>> GetCollectionsByMapAsync(BeatmapSettings beatmapSettings)
        {
            LogTemporaryMap(beatmapSettings);

            return await CollectionRelations
                .Where(relation => relation.MapId == beatmapSettings.Id)
                .Join(Collections,
                    relation => relation.CollectionId,
                    collection => collection.Id,
                    (_, collection) => collection)
                .ToListAsync();
        }

        public async Task AddCollectionAsync(string name, bool locked = false)
        {
            Collections.Add(new Collection
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Locked = locked ? 1 : 0,
                Index = 0,
                CreateTime = DateTime.Now
            });
            await SaveChangesAsync();
        }

        public async Task<Collection> GetCollectionByIdAsync(string id)
        {
            return await Collections.FirstOrDefaultAsync(k => k.Id == id);
        }

        public async Task AddMapsToCollectionAsync(IList<Beatmap> beatmaps, Collection collection)
        {
            if (beatmaps.Count < 1) return;

            var relations = new List<CollectionRelation>(beatmaps.Count);
            var addTime = DateTime.Now;
            var existingMapIds = await CollectionRelations
                .Where(k => k.CollectionId == collection.Id)
                .Select(k => k.MapId)
                .ToHashSetAsync();

            var queryKeys = beatmaps.Select(b => b.GetIdentity()).ToList();
            var settingsLookup = await BeatmapSettings
                .Where(s => queryKeys.Any(id =>
                    s.FolderName == id.FolderName && s.Version == id.Version && s.InOwnDb == id.InOwnDb))
                .ToDictionaryAsync(s => $"{s.FolderName}|{s.Version}|{s.InOwnDb}");

            var newSettings = new List<BeatmapSettings>();
            foreach (var beatmap in beatmaps)
            {
                LogTemporaryMap(beatmap);
                var key = $"{beatmap.FolderName}|{beatmap.Version}|{beatmap.InOwnDb}";

                if (!settingsLookup.TryGetValue(key, out var map))
                {
                    map = new BeatmapSettings
                    {
                        Id = Guid.NewGuid().ToString(),
                        Version = beatmap.Version,
                        FolderName = beatmap.FolderName,
                        InOwnDb = beatmap.InOwnDb,
                        Offset = 0
                    };
                    newSettings.Add(map);
                    settingsLookup[key] = map;
                }

                if (existingMapIds.Contains(map.Id))
                {
                    continue;
                }

                relations.Add(new CollectionRelation
                {
                    Id = Guid.NewGuid().ToString(),
                    CollectionId = collection.Id,
                    MapId = map.Id,
                    AddTime = addTime
                });
                existingMapIds.Add(map.Id);
            }

            if (newSettings.Count > 0)
            {
                BeatmapSettings.AddRange(newSettings);
            }

            CollectionRelations.AddRange(relations);
            await SaveChangesAsync();
        }

        public async Task UpdateCollectionAsync(Collection collection)
        {
            var result = await Collections.FirstOrDefaultAsync(k => k.Id == collection.Id);
            if (result == null)
            {
                await AddCollectionAsync(collection);
                return;
            }

            result.Name = collection.Name;
            result.Locked = collection.LockedBool ? 1 : 0;
            result.Index = collection.Index;
            result.ImagePath = collection.ImagePath;
            result.Description = collection.Description;
            result.CreateTime = collection.CreateTime;
            await SaveChangesAsync();
        }

        public async Task UpdateMapAsync(IMapIdentifiable id, int? offset = null)
        {
            var updateColumns = new Action<BeatmapSettings>(map => map.LastPlayTime = DateTime.Now);
            if (offset != null)
            {
                updateColumns += map => map.Offset = offset.Value;
            }

            await InnerUpdateMapAsync(id, updateColumns);
        }

        public async Task AddMapExportAsync(IMapIdentifiable id, string exportFilePath)
        {
            await InnerUpdateMapAsync(id, map => map.ExportFile = exportFilePath);
        }

        public async Task RemoveMapExportAsync(IMapIdentifiable id)
        {
            await InnerUpdateMapAsync(id, map => map.ExportFile = null);
        }

        public async Task RemoveFromRecentAsync(IMapIdentifiable id)
        {
            await InnerUpdateMapAsync(id, map => map.LastPlayTime = null);
        }

        public async Task ClearRecentAsync()
        {
            await BeatmapSettings.ExecuteUpdateAsync(setters =>
                setters.SetProperty(map => map.LastPlayTime, (DateTime?)null));
        }

        public async Task RemoveCollectionAsync(Collection collection)
        {
            using var transaction = await Database.BeginTransactionAsync();
            await Collections.Where(k => k.Id == collection.Id).ExecuteDeleteAsync();
            await CollectionRelations.Where(k => k.CollectionId == collection.Id).ExecuteDeleteAsync();
            await transaction.CommitAsync();
        }

        public async Task RemoveMapFromCollectionAsync(IMapIdentifiable id, Collection collection)
        {
            LogTemporaryMap(id);

            var map = await GetMapFromDbAsync(id);
            await CollectionRelations
                .Where(k => k.CollectionId == collection.Id && k.MapId == map.Id)
                .ExecuteDeleteAsync();
        }

        public async Task<(bool found, string thumbPath)> GetMapThumbAsync(Guid beatmapDbId)
        {
            var thumb = await MapThumbs.FirstOrDefaultAsync(k => k.MapId == beatmapDbId);
            return (thumb != null, thumb?.ThumbPath);
        }

        public async Task<(bool found, string thumbPath)> GetMapThumbAsync(Beatmap beatmap)
        {
            LogTemporaryMap(beatmap);
            return await GetMapThumbAsync(beatmap.Id);
        }

        public async Task SetMapThumbAsync(Guid beatmapDbId, string thumbPath)
        {
            var dbConnection = Database.GetDbConnection();
            var shouldClose = dbConnection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await dbConnection.OpenAsync();
            }

            try
            {
                const string sql = @"
INSERT OR REPLACE INTO beatmap_thumbnails (id, beatmap_id, thumbnail_path)
VALUES ((SELECT id FROM beatmap_thumbnails WHERE beatmap_id = @MapId), @MapId, @ThumbPath);";
                await dbConnection.ExecuteAsync(sql, new { MapId = beatmapDbId.ToString(), ThumbPath = thumbPath });
            }
            finally
            {
                if (shouldClose)
                {
                    await dbConnection.CloseAsync();
                }
            }
        }

        public async Task SetMapThumbAsync(Beatmap beatmap, string thumbPath)
        {
            await SetMapThumbAsync(beatmap.Id, thumbPath);
        }

        public async Task SetMapSbInfoAsync(Guid beatmapDbId, StoryboardInfo sbInfo)
        {
            LogTemporaryMap(sbInfo);

            var dbConnection = Database.GetDbConnection();
            var shouldClose = dbConnection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await dbConnection.OpenAsync();
            }

            try
            {
                const string sql = @"
INSERT OR REPLACE INTO storyboard_assets (id, beatmap_id, thumbnail_path, preview_video_path, difficulty_name, folder_name, is_local)
VALUES ((SELECT id FROM storyboard_assets WHERE beatmap_id = @MapId), @MapId, @SbThumbPath, @SbThumbVideoPath, @Version, @FolderName, @InOwnDb);";
                await dbConnection.ExecuteAsync(sql, new
                {
                    MapId = beatmapDbId.ToString(),
                    SbThumbPath = sbInfo.SbThumbPath,
                    SbThumbVideoPath = sbInfo.SbThumbVideoPath,
                    sbInfo.Version,
                    sbInfo.FolderName,
                    InOwnDb = sbInfo.InOwnDb ? 1 : 0
                });
            }
            finally
            {
                if (shouldClose)
                {
                    await dbConnection.CloseAsync();
                }
            }
        }

        public async Task SetMapSbInfoAsync(Beatmap beatmap, StoryboardInfo sbInfo)
        {
            await SetMapSbInfoAsync(beatmap.Id, sbInfo);
        }

        public async Task<List<Beatmap>> SearchBeatmapByOptionsAsync(string searchText, BeatmapSortMode beatmapSortMode,
            int startIndex, int count)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                IQueryable<Beatmap> query = Beatmaps.AsNoTracking();

                var keywords = searchText?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (keywords != null)
                {
                    foreach (var keyword in keywords)
                    {
                        var pattern = $"%{keyword}%";
                        query = query.Where(k =>
                            EF.Functions.Like(k.Artist, pattern) ||
                            EF.Functions.Like(k.ArtistUnicode, pattern) ||
                            EF.Functions.Like(k.Title, pattern) ||
                            EF.Functions.Like(k.TitleUnicode, pattern) ||
                            EF.Functions.Like(k.SongTags, pattern) ||
                            EF.Functions.Like(k.SongSource, pattern) ||
                            EF.Functions.Like(k.Creator, pattern) ||
                            EF.Functions.Like(k.Version, pattern));
                    }
                }

                query = beatmapSortMode switch
                {
                    BeatmapSortMode.Title => query
                        .OrderBy(k => k.TitleUnicode)
                        .ThenBy(k => k.Title),
                    _ => query
                        .OrderBy(k => k.ArtistUnicode)
                        .ThenBy(k => k.Artist)
                };

                return await query
                    .Skip(startIndex)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling SearchBeatmapByOptionsAsync().");
                throw;
            }
            finally
            {
                Logger.Debug("查询花费: {0}", sw.ElapsedMilliseconds);
                sw.Stop();
            }
        }

        public async Task<List<Beatmap>> GetAllBeatmapsAsync()
        {
            try
            {
                return await Beatmaps.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetAllBeatmapsAsync().");
                throw;
            }
        }

        public async Task<Beatmap> GetBeatmapByIdentifiableAsync(IMapIdentifiable id)
        {
            try
            {
                return await Beatmaps.AsNoTracking()
                    .Where(k => k.Version == id.Version && k.FolderName == id.FolderName)
                    .OrderByDescending(k => k.InOwnDb == id.InOwnDb)
                    .FirstOrDefaultAsync() ?? await Beatmaps.AsNoTracking()
                    .Where(k => k.Version == id.Version && k.FolderName == id.FolderName)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapByIdentifiableAsync().");
                throw;
            }
        }

        public async Task<List<Beatmap>> GetBeatmapsByMapInfoAsync(List<BeatmapSettings> reqList, TimeSortMode sortMode)
        {
            var entities = await GetBeatmapsByIdentifiableAsync(reqList);

            var newList = reqList.Join(entities,
                mapInfo => mapInfo.GetIdentity(),
                entry => entry.GetIdentity(),
                (mapInfo, entry) => new
                {
                    entry,
                    playTime = mapInfo.LastPlayTime ?? new DateTime(),
                    addTime = mapInfo.AddTime ?? new DateTime()
                });

            return sortMode == TimeSortMode.PlayTime
                ? newList.OrderByDescending(k => k.playTime).Select(k => k.entry).ToList()
                : newList.OrderByDescending(k => k.addTime).Select(k => k.entry).ToList();
        }

        public async Task<List<Beatmap>> GetBeatmapsFromFolderAsync(string folder)
        {
            try
            {
                return await Beatmaps.AsNoTracking()
                    .Where(k => k.FolderName == folder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsFromFolderAsync().");
                throw;
            }
        }

        public async Task<List<Beatmap>> GetBeatmapsByIdentifiableAsync<T>(IEnumerable<T> reqList)
            where T : IMapIdentifiable
        {
            var identities = reqList
                .Where(k => !k.IsMapTemporary())
                .Where(k => !(k is MapIdentity mi && mi.Equals(MapIdentity.Default)))
                .Select(k => new
                {
                    k.FolderName,
                    k.Version,
                    OwnDb = k.InOwnDb ? 1 : 0
                })
                .ToList();

            if (identities.Count == 0) return new List<Beatmap>();

            try
            {
                var dbConnection = Database.GetDbConnection();
                var shouldClose = dbConnection.State != ConnectionState.Open;
                if (shouldClose)
                {
                    await dbConnection.OpenAsync();
                }

                try
                {
                    var result = new List<Beatmap>(identities.Count);
                    foreach (var chunk in identities.Chunk(MaxIdentitiesPerQuery))
                    {
                        var sql = new StringBuilder(@"
 WITH ids(folder_name, difficulty_name, is_local, ord) AS (
     VALUES ");
                        var parameters = new DynamicParameters();

                        for (var i = 0; i < chunk.Length; i++)
                        {
                            if (i > 0)
                            {
                                sql.Append(", ");
                            }

                            sql.Append($"(@folder{i}, @version{i}, @ownDb{i}, @ord{i})");
                            parameters.Add($"folder{i}", chunk[i].FolderName);
                            parameters.Add($"version{i}", chunk[i].Version);
                            parameters.Add($"ownDb{i}", chunk[i].OwnDb);
                            parameters.Add($"ord{i}", i);
                        }

                        sql.Append(@"
 )
 SELECT b.*
   FROM ids
        INNER JOIN
        beatmaps AS b ON b.folder_name = ids.folder_name AND 
                        b.difficulty_name = ids.difficulty_name AND 
                        b.is_local = ids.is_local
  ORDER BY ids.ord;");

                        result.AddRange(await dbConnection.QueryAsync<Beatmap>(sql.ToString(), parameters));
                    }

                    return result;
                }
                finally
                {
                    if (shouldClose)
                    {
                        await dbConnection.CloseAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsByIdentifiableAsync().");
                throw;
            }
        }

        public async Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> newList, bool addOnly)
        {
            try
            {
                if (addOnly)
                {
                    var dbMaps = await Beatmaps.AsNoTracking()
                        .Where(k => !k.InOwnDb)
                        .ToListAsync();
                    var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                    await AddNewMapsAsync(except);
                }
                else
                {
                    await RemoveSyncedAllAsync();
                    await AddNewMapsAsync(newList);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, addOnly
                    ? "Error while calling SyncMapsFromHoLLyAsync(addonly)."
                    : "Error while calling SyncMapsFromHoLLyAsync().");
                throw;
            }
        }

        public async Task AddNewMapsAsync(IEnumerable<Beatmap> beatmaps)
        {
            const string sql = @"
INSERT OR IGNORE INTO beatmaps (
    id,
    artist,
    artist_unicode,
    title,
    title_unicode,
    creator,
    difficulty_name,
    beatmap_file_name,
    last_modified_at,
    star_rating_standard,
    star_rating_taiko,
    star_rating_catch,
    star_rating_mania,
    drain_time_seconds,
    total_time_ms,
    preview_time_ms,
    osu_beatmap_id,
    osu_beatmapset_id,
    game_mode,
    source,
    tags,
    folder_name,
    audio_file_name,
    is_local
) VALUES (
    @Id,
    @Artist,
    @ArtistUnicode,
    @Title,
    @TitleUnicode,
    @Creator,
    @Version,
    @BeatmapFileName,
    @LastModifiedTime,
    @DiffSrNoneStandard,
    @DiffSrNoneTaiko,
    @DiffSrNoneCtB,
    @DiffSrNoneMania,
    @DrainTimeSeconds,
    @TotalTime,
    @AudioPreviewTime,
    @BeatmapId,
    @BeatmapSetId,
    @GameMode,
    @SongSource,
    @SongTags,
    @FolderName,
    @AudioFileName,
    @InOwnDb
);";

            var rows = beatmaps.Select(k => new
            {
                k.Id,
                k.Artist,
                k.ArtistUnicode,
                k.Title,
                k.TitleUnicode,
                k.Creator,
                k.Version,
                k.BeatmapFileName,
                k.LastModifiedTime,
                k.DiffSrNoneStandard,
                k.DiffSrNoneTaiko,
                k.DiffSrNoneCtB,
                k.DiffSrNoneMania,
                k.DrainTimeSeconds,
                k.TotalTime,
                k.AudioPreviewTime,
                k.BeatmapId,
                k.BeatmapSetId,
                GameMode = (int)k.GameMode,
                k.SongSource,
                k.SongTags,
                k.FolderName,
                k.AudioFileName,
                k.InOwnDb
            }).ToList();

            if (rows.Count == 0)
            {
                return;
            }

            var dbConnection = Database.GetDbConnection();
            var shouldClose = dbConnection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await dbConnection.OpenAsync();
            }

            try
            {
                using var transaction = await dbConnection.BeginTransactionAsync();
                await dbConnection.ExecuteAsync(sql, rows, transaction);
                await transaction.CommitAsync();
            }
            finally
            {
                if (shouldClose)
                {
                    await dbConnection.CloseAsync();
                }
            }
        }

        public async Task AddNewMapsAsync(params Beatmap[] beatmaps)
        {
            await AddNewMapsAsync((IEnumerable<Beatmap>)beatmaps);
        }

        public async Task RemoveLocalAllAsync()
        {
            await Beatmaps.Where(k => k.InOwnDb).ExecuteDeleteAsync();
        }

        public async Task RemoveSyncedAllAsync()
        {
            await Beatmaps.Where(k => !k.InOwnDb).ExecuteDeleteAsync();
        }

        private async Task AddCollectionAsync(Collection collection)
        {
            Collections.Add(new Collection
            {
                Id = string.IsNullOrWhiteSpace(collection.Id) ? Guid.NewGuid().ToString() : collection.Id,
                Name = collection.Name,
                Locked = collection.LockedBool ? 1 : 0,
                Index = collection.Index,
                ImagePath = collection.ImagePath,
                Description = collection.Description,
                CreateTime = collection.CreateTime == default ? DateTime.Now : collection.CreateTime
            });
            await SaveChangesAsync();
        }

        private async Task InnerUpdateMapAsync(IMapIdentifiable id, Action<BeatmapSettings> updateAction)
        {
            LogTemporaryMap(id);

            try
            {
                var map = await BeatmapSettings.FirstOrDefaultAsync(k =>
                    k.Version == id.Version &&
                    k.FolderName == id.FolderName &&
                    k.InOwnDb == id.InOwnDb);

                if (map == null)
                {
                    map = new BeatmapSettings
                    {
                        Id = Guid.NewGuid().ToString(),
                        Version = id.Version,
                        FolderName = id.FolderName,
                        InOwnDb = id.InOwnDb,
                        Offset = 0
                    };
                    BeatmapSettings.Add(map);
                }

                updateAction(map);
                await SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling InnerUpdateMapAsync().");
                throw;
            }
        }

        private static void LogTemporaryMap(IMapIdentifiable id)
        {
            if (id.IsMapTemporary())
            {
                Logger.Debug("需确认加入自定义目录后才可继续");
            }
        }

        private static void ConfigureDapperCompatibility()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid value)
            {
                parameter.Value = value.ToString();
            }

            public override Guid Parse(object value)
            {
                return value switch
                {
                    Guid guid => guid,
                    string text => Guid.Parse(text),
                    byte[] bytes => new Guid(bytes),
                    _ => Guid.Parse(value.ToString())
                };
            }
        }
    }
}