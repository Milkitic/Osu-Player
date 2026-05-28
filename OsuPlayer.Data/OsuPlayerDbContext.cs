using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
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

            entity.ToTable("beatmap");
            entity.HasKey(k => k.Id);
            entity.HasIndex(k => new { k.FolderName, k.Version, k.InOwnDb })
                .HasDatabaseName("IX_beatmap_identity");
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Artist).HasColumnName("artist");
            entity.Property(k => k.ArtistUnicode).HasColumnName("artistU");
            entity.Property(k => k.Title).HasColumnName("title");
            entity.Property(k => k.TitleUnicode).HasColumnName("titleU");
            entity.Property(k => k.Creator).HasColumnName("creator");
            entity.Property(k => k.Version).HasColumnName("version");
            entity.Property(k => k.BeatmapFileName).HasColumnName("fileName");
            entity.Property(k => k.LastModifiedTime).HasColumnName("lastModified");
            entity.Property(k => k.DiffSrNoneStandard).HasColumnName("diffSrStd");
            entity.Property(k => k.DiffSrNoneTaiko).HasColumnName("diffSrTaiko");
            entity.Property(k => k.DiffSrNoneCtB).HasColumnName("diffSrCtb");
            entity.Property(k => k.DiffSrNoneMania).HasColumnName("diffSrMania");
            entity.Property(k => k.DrainTimeSeconds).HasColumnName("drainTime");
            entity.Property(k => k.TotalTime).HasColumnName("totalTime");
            entity.Property(k => k.AudioPreviewTime).HasColumnName("audioPreview");
            entity.Property(k => k.BeatmapId).HasColumnName("beatmapId");
            entity.Property(k => k.BeatmapSetId).HasColumnName("beatmapSetId");
            entity.Property(k => k.GameMode).HasColumnName("gameMode");
            entity.Property(k => k.SongSource).HasColumnName("source");
            entity.Property(k => k.SongTags).HasColumnName("tags");
            entity.Property(k => k.FolderName).HasColumnName("folderName");
            entity.Property(k => k.AudioFileName).HasColumnName("audioName");
            entity.Property(k => k.InOwnDb).HasColumnName("own");

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

            entity.ToTable("map_info");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Version).HasColumnName("version").IsRequired();
            entity.Property(k => k.FolderName).HasColumnName("folder").IsRequired();
            entity.Property(k => k.InOwnDb).HasColumnName("ownDb");
            entity.Property(k => k.Offset).HasColumnName("offset");
            entity.Property(k => k.LastPlayTime).HasColumnName("lastPlayTime");
            entity.Property(k => k.ExportFile).HasColumnName("exportFile");

            entity.Ignore(k => k.AddTime);
        }

        private static void ConfigureCollection(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Collection>();

            entity.ToTable("collection");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
            entity.Property(k => k.Locked).HasColumnName("locked");
            entity.Property(k => k.Index).HasColumnName("index");
            entity.Property(k => k.ImagePath).HasColumnName("imagePath").HasMaxLength(700);
            entity.Property(k => k.Description).HasColumnName("description").HasMaxLength(700);
            entity.Property(k => k.CreateTime).HasColumnName("createTime");

            entity.Ignore(k => k.LockedBool);
        }

        private static void ConfigureCollectionRelation(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<CollectionRelation>();

            entity.ToTable("collection_relation");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.CollectionId).HasColumnName("collectionId").IsRequired();
            entity.Property(k => k.MapId).HasColumnName("mapId").IsRequired();
            entity.Property(k => k.AddTime).HasColumnName("addTime");
        }

        private static void ConfigureMapThumb(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<MapThumb>();

            entity.ToTable("map_thumb");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.MapId).HasColumnName("mapId");
            entity.Property(k => k.ThumbPath).HasColumnName("thumbPath");
        }

        private static void ConfigureStoryboardInfo(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<StoryboardInfo>();

            entity.ToTable("sb_info");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).HasColumnName("id");
            entity.Property(k => k.MapId).HasColumnName("mapId").IsRequired();
            entity.Property(k => k.SbThumbPath).HasColumnName("thumbPath").IsRequired();
            entity.Property(k => k.SbThumbVideoPath).HasColumnName("thumbVideoPath").IsRequired();
            entity.Property(k => k.Version).HasColumnName("version").IsRequired();
            entity.Property(k => k.FolderName).HasColumnName("folder").IsRequired();
            entity.Property(k => k.InOwnDb).HasColumnName("own");
        }

        public static void InitializeDatabase()
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                db.Database.Migrate();
                LegacyPlayerDatabaseMigrator.MigrateIfRequired(DefaultDatabasePath, LegacyDatabasePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while initializing local database.");
                throw;
            }
        }

        public static void ValidateDb()
        {
            InitializeDatabase();
        }

        public BeatmapSettings GetMapFromDb(IMapIdentifiable id)
        {
            try
            {
                LogTemporaryMap(id);

                var map = BeatmapSettings.FirstOrDefault(k =>
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
                SaveChanges();
                return map;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetMapFromDb().");
                throw;
            }
        }

        public List<BeatmapSettings> GetRecentList()
        {
            return BeatmapSettings
                .Where(k => k.LastPlayTime != null)
                .OrderBy(k => k.LastPlayTime)
                .ToList();
        }

        public List<BeatmapSettings> GetExportedMaps()
        {
            return BeatmapSettings
                .Where(k => k.ExportFile != null && k.ExportFile.Trim() != string.Empty)
                .ToList();
        }

        public List<BeatmapSettings> GetMapsFromCollection(Collection collection)
        {
            return CollectionRelations
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
                .ToList();
        }

        public List<Collection> GetCollections()
        {
            return Collections.ToList();
        }

        public List<Collection> GetCollectionsByMap(BeatmapSettings beatmapSettings)
        {
            LogTemporaryMap(beatmapSettings);

            return CollectionRelations
                .Where(relation => relation.MapId == beatmapSettings.Id)
                .Join(Collections,
                    relation => relation.CollectionId,
                    collection => collection.Id,
                    (_, collection) => collection)
                .ToList();
        }

        public void AddCollection(string name, bool locked = false)
        {
            Collections.Add(new Collection
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Locked = locked ? 1 : 0,
                Index = 0,
                CreateTime = DateTime.Now
            });
            SaveChanges();
        }

        public Collection GetCollectionById(string id)
        {
            return Collections.FirstOrDefault(k => k.Id == id);
        }

        public void AddMapsToCollection(IList<Beatmap> beatmaps, Collection collection)
        {
            if (beatmaps.Count < 1) return;

            var relations = new List<CollectionRelation>(beatmaps.Count);
            var addTime = DateTime.Now;
            foreach (var beatmap in beatmaps)
            {
                LogTemporaryMap(beatmap);

                var map = GetMapFromDb(beatmap.GetIdentity());
                relations.Add(new CollectionRelation
                {
                    Id = Guid.NewGuid().ToString(),
                    CollectionId = collection.Id,
                    MapId = map.Id,
                    AddTime = addTime
                });
            }

            CollectionRelations.AddRange(relations);
            SaveChanges();
        }

        public void UpdateCollection(Collection collection)
        {
            var result = Collections.FirstOrDefault(k => k.Id == collection.Id);
            if (result == null)
            {
                AddCollection(collection);
                return;
            }

            result.Name = collection.Name;
            result.Locked = collection.LockedBool ? 1 : 0;
            result.Index = collection.Index;
            result.ImagePath = collection.ImagePath;
            result.Description = collection.Description;
            result.CreateTime = collection.CreateTime;
            SaveChanges();
        }

        public void UpdateMap(IMapIdentifiable id, int? offset = null)
        {
            var updateColumns = new Action<BeatmapSettings>(map => map.LastPlayTime = DateTime.Now);
            if (offset != null)
            {
                updateColumns += map => map.Offset = offset.Value;
            }

            InnerUpdateMap(id, updateColumns);
        }

        public void AddMapExport(IMapIdentifiable id, string exportFilePath)
        {
            InnerUpdateMap(id, map => map.ExportFile = exportFilePath);
        }

        public void RemoveMapExport(IMapIdentifiable id)
        {
            InnerUpdateMap(id, map => map.ExportFile = null);
        }

        public void RemoveFromRecent(IMapIdentifiable id)
        {
            InnerUpdateMap(id, map => map.LastPlayTime = null);
        }

        public void ClearRecent()
        {
            BeatmapSettings.ExecuteUpdate(setters => setters.SetProperty(map => map.LastPlayTime, (DateTime?)null));
        }

        public void RemoveCollection(Collection collection)
        {
            using var transaction = Database.BeginTransaction();
            Collections.Where(k => k.Id == collection.Id).ExecuteDelete();
            CollectionRelations.Where(k => k.CollectionId == collection.Id).ExecuteDelete();
            transaction.Commit();
        }

        public void RemoveMapFromCollection(IMapIdentifiable id, Collection collection)
        {
            LogTemporaryMap(id);

            var map = GetMapFromDb(id);
            CollectionRelations
                .Where(k => k.CollectionId == collection.Id && k.MapId == map.Id)
                .ExecuteDelete();
        }

        public bool GetMapThumb(Guid beatmapDbId, out string thumbPath)
        {
            var thumb = MapThumbs.FirstOrDefault(k => k.MapId == beatmapDbId);
            thumbPath = thumb?.ThumbPath;
            return thumb != null;
        }

        public bool GetMapThumb(Beatmap beatmap, out string thumbPath)
        {
            LogTemporaryMap(beatmap);

            return GetMapThumb(beatmap.Id, out thumbPath);
        }

        public void SetMapThumb(Guid beatmapDbId, string thumbPath)
        {
            var thumb = MapThumbs.FirstOrDefault(k => k.MapId == beatmapDbId);
            if (thumb == null)
            {
                MapThumbs.Add(new MapThumb
                {
                    Id = Guid.NewGuid().ToString(),
                    MapId = beatmapDbId,
                    ThumbPath = thumbPath
                });
            }
            else
            {
                thumb.ThumbPath = thumbPath;
            }

            SaveChanges();
        }

        public void SetMapThumb(Beatmap beatmap, string thumbPath)
        {
            SetMapThumb(beatmap.Id, thumbPath);
        }

        public void SetMapSbInfo(Guid beatmapDbId, StoryboardInfo sbInfo)
        {
            LogTemporaryMap(sbInfo);

            var mapId = beatmapDbId.ToString();
            var result = StoryboardInfos.FirstOrDefault(k => k.MapId == mapId);
            if (result == null)
            {
                StoryboardInfos.Add(new StoryboardInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    MapId = mapId,
                    SbThumbPath = sbInfo.SbThumbPath,
                    SbThumbVideoPath = sbInfo.SbThumbVideoPath,
                    Version = sbInfo.Version,
                    FolderName = sbInfo.FolderName,
                    InOwnDb = sbInfo.InOwnDb
                });
            }
            else
            {
                result.SbThumbPath = sbInfo.SbThumbPath;
                result.SbThumbVideoPath = sbInfo.SbThumbVideoPath;
                result.Version = sbInfo.Version;
                result.FolderName = sbInfo.FolderName;
                result.InOwnDb = sbInfo.InOwnDb;
            }

            SaveChanges();
        }

        public void SetMapSbInfo(Beatmap beatmap, StoryboardInfo sbInfo)
        {
            SetMapSbInfo(beatmap.Id, sbInfo);
        }

        public List<Beatmap> SearchBeatmapByOptions(string searchText, BeatmapSortMode beatmapSortMode, int startIndex, int count)
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

                return query
                    .Skip(startIndex)
                    .Take(count)
                    .ToList();
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

        public List<Beatmap> GetAllBeatmaps()
        {
            try
            {
                return Beatmaps.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetAllBeatmaps().");
                throw;
            }
        }

        public Beatmap GetBeatmapByIdentifiable(IMapIdentifiable id)
        {
            try
            {
                var query = Beatmaps.AsNoTracking()
                    .Where(k => k.Version == id.Version && k.FolderName == id.FolderName);

                return query.FirstOrDefault(k => k.InOwnDb == id.InOwnDb) ??
                    query.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapByIdentifiable().");
                throw;
            }
        }

        public List<Beatmap> GetBeatmapsByMapInfo(List<BeatmapSettings> reqList, TimeSortMode sortMode)
        {
            var entities = GetBeatmapsByIdentifiable(reqList);

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

        public List<Beatmap> GetBeatmapsFromFolder(string folder)
        {
            try
            {
                return Beatmaps.AsNoTracking()
                    .Where(k => k.FolderName == folder)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsFromFolder().");
                throw;
            }
        }

        public List<Beatmap> GetBeatmapsByIdentifiable<T>(IEnumerable<T> reqList)
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
                    dbConnection.Open();
                }

                try
                {
                    var result = new List<Beatmap>(identities.Count);
                    foreach (var chunk in identities.Chunk(MaxIdentitiesPerQuery))
                    {
                        var sql = new StringBuilder(@"
WITH ids(folder, version, ownDb, ord) AS (
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
       beatmap AS b ON b.folderName = ids.folder AND 
                     b.version = ids.version AND 
                     b.own = ids.ownDb
 ORDER BY ids.ord;");

                        result.AddRange(dbConnection.Query<Beatmap>(sql.ToString(), parameters));
                    }

                    return result;
                }
                finally
                {
                    if (shouldClose)
                    {
                        dbConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsByIdentifiable().");
                throw;
            }
        }

        public Task SyncMapsFromOsuDbAsync(IEnumerable<Beatmap> newList, bool addOnly)
        {
            try
            {
                if (addOnly)
                {
                    var dbMaps = Beatmaps.AsNoTracking()
                        .Where(k => !k.InOwnDb)
                        .ToList();
                    var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                    AddNewMaps(except);
                }
                else
                {
                    RemoveSyncedAll();
                    AddNewMaps(newList);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, addOnly
                    ? "Error while calling SyncMapsFromHoLLyAsync(addonly)."
                    : "Error while calling SyncMapsFromHoLLyAsync().");
                throw;
            }
        }

        public void AddNewMaps(IEnumerable<Beatmap> beatmaps)
        {
            const string sql = @"
INSERT INTO beatmap (
    id,
    artist,
    artistU,
    title,
    titleU,
    creator,
    version,
    fileName,
    lastModified,
    diffSrStd,
    diffSrTaiko,
    diffSrCtb,
    diffSrMania,
    drainTime,
    totalTime,
    audioPreview,
    beatmapId,
    beatmapSetId,
    gameMode,
    source,
    tags,
    folderName,
    audioName,
    own
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
                dbConnection.Open();
            }

            try
            {
                using var transaction = dbConnection.BeginTransaction();
                dbConnection.Execute(sql, rows, transaction);
                transaction.Commit();
            }
            finally
            {
                if (shouldClose)
                {
                    dbConnection.Close();
                }
            }
        }

        public void AddNewMaps(params Beatmap[] beatmaps)
        {
            AddNewMaps((IEnumerable<Beatmap>)beatmaps);
        }

        public void RemoveLocalAll()
        {
            Beatmaps.Where(k => k.InOwnDb).ExecuteDelete();
        }

        public void RemoveSyncedAll()
        {
            Beatmaps.Where(k => !k.InOwnDb).ExecuteDelete();
        }

        private void AddCollection(Collection collection)
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
            SaveChanges();
        }

        private void InnerUpdateMap(IMapIdentifiable id, Action<BeatmapSettings> updateAction)
        {
            LogTemporaryMap(id);

            try
            {
                var map = BeatmapSettings.FirstOrDefault(k =>
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
                SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling InnerUpdateMap().");
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
