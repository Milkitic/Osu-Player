using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
    public static class BeatmapDbOperator
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private const int MaxIdentitiesPerQuery = 300;

        public static List<Beatmap> SearchBeatmapByOptions(this AppDbOperator op, string searchText, BeatmapSortMode beatmapSortMode, int startIndex, int count)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var db = new OsuPlayerDbContext();
                IQueryable<Beatmap> query = db.Beatmaps.AsNoTracking();

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

        public static List<Beatmap> GetAllBeatmaps(this AppDbOperator op)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.Beatmaps.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetAllBeatmaps().");
                throw;
            }
        }

        public static Beatmap GetBeatmapByIdentifiable(this AppDbOperator op, IMapIdentifiable id)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                var query = db.Beatmaps.AsNoTracking()
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

        public static List<Beatmap> GetBeatmapsByMapInfo(this AppDbOperator op, List<BeatmapSettings> reqList, TimeSortMode sortMode)
        {
            var entities = GetBeatmapsByIdentifiable(op, reqList);

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

        public static List<Beatmap> GetBeatmapsFromFolder(this AppDbOperator op, string folder)
        {
            try
            {
                using var db = new OsuPlayerDbContext();
                return db.Beatmaps.AsNoTracking()
                    .Where(k => k.FolderName == folder)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsFromFolder().");
                throw;
            }
        }

        public static List<Beatmap> GetBeatmapsByIdentifiable<T>(this AppDbOperator op, IEnumerable<T> reqList)
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
                var dbConnection = op.GetDapperConnection();
                if (dbConnection.State != ConnectionState.Open)
                {
                    dbConnection.Open();
                }

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
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while calling GetBeatmapsByIdentifiable().");
                throw;
            }
        }

        // todo: to be optimized
        public static async Task SyncMapsFromOsuDbAsync(this AppDbOperator op, IEnumerable<Beatmap> newList, bool addOnly)
        {
            Exception exc = null;
            if (addOnly)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using var db = new OsuPlayerDbContext();
                        var dbMaps = db.Beatmaps.AsNoTracking()
                            .Where(k => !k.InOwnDb)
                            .ToList();
                        var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                        AddNewMaps(op, except);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error while calling SyncMapsFromHoLLyAsync(addonly).");
                        exc = ex;
                    }
                }).ConfigureAwait(false);
            }
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        RemoveSyncedAll(op);
                        AddNewMaps(op, newList);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error while calling SyncMapsFromHoLLyAsync().");
                        exc = ex;
                    }
                }).ConfigureAwait(false);
            }

            if (exc != null) throw exc;
        }

        public static void AddNewMaps(this AppDbOperator op, IEnumerable<Beatmap> beatmaps)
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

            var dbConnection = op.GetDapperConnection();
            if (dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaction = dbConnection.BeginTransaction();
            dbConnection.Execute(sql, rows, transaction);
            transaction.Commit();
        }

        public static void AddNewMaps(this AppDbOperator op, params Beatmap[] beatmaps)
        {
            AddNewMaps(op, (IEnumerable<Beatmap>)beatmaps);
        }

        public static void RemoveLocalAll(this AppDbOperator op)
        {
            using var db = new OsuPlayerDbContext();
            db.Beatmaps.Where(k => k.InOwnDb).ExecuteDelete();
        }

        public static void RemoveSyncedAll(this AppDbOperator op)
        {
            using var db = new OsuPlayerDbContext();
            db.Beatmaps.Where(k => !k.InOwnDb).ExecuteDelete();
        }
    }

    public enum TimeSortMode
    {
        PlayTime, AddTime
    }
}
