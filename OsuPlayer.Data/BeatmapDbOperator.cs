using Dapper;
using Milky.OsuPlayer.Data.Dapper;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Shared;
using OSharp.Beatmap.MetaData;
using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Shared.Models;

namespace Milky.OsuPlayer.Data
{
    public static class BeatmapDbOperator
    {
        private const string TABLE_BEATMAP = AppDbOperator.TABLE_BEATMAP;

        public static List<Beatmap> SearchBeatmapByOptions(this AppDbOperator op, string searchText, BeatmapSortMode beatmapSortMode, int startIndex, int count)
        {
            var expando = new DynamicParameters();
            var command = " SELECT * FROM beatmap WHERE ";
            var keywordSql = GetKeywordQueryAndArgs(searchText, ref expando);
            var sort = GetOrderAndTakeQueryAndArgs(beatmapSortMode, startIndex, count);
            var sw = Stopwatch.StartNew();
            try
            {
                return op.ThreadedProvider.GetDbConnection().Query<Beatmap>(command + keywordSql + sort, expando).ToList();
            }
            finally
            {
                Console.WriteLine($"query: {sw.ElapsedMilliseconds}");
                sw.Stop();
            }
        }

        public static List<Beatmap> GetAllBeatmaps(this AppDbOperator op)
        {
            return op.ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP).ToList();
        }

        public static Beatmap GetBeatmapByIdentifiable(this AppDbOperator op, IMapIdentifiable id)
        {
            return op.ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP,
                new Where[]
                {
                    ("version", id.Version),
                    ("folderName", id.FolderName)
                },
                count: 1).FirstOrDefault();
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
            return op.ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP, ("folderName", folder)).ToList();
        }

        public static List<Beatmap> GetBeatmapsByIdentifiable<T>(this AppDbOperator op, ICollection<T> reqList)
            where T : IMapIdentifiable
        {
            if (reqList.Count < 1) return new List<Beatmap>();

            var inDbMaps = reqList.Where(k => !k.IsMapTemporary()).ToList();
            //var absMaps = reqList.Except(inDbMaps).ToList();
            //var args = new ExpandoObject();
            //var expando = (ICollection<KeyValuePair<string, object>>)args;

            var sb = new StringBuilder();
            foreach (var id in inDbMaps)
            {
                if (id is MapIdentity mi && mi.Equals(MapIdentity.Default)) continue;
                var valueSql = string.Format("('{0}', '{1}', {2}),",
                    id.FolderName.Replace(@"'", @"''"),
                    id.Version.Replace(@"'", @"''"),
                    id.InOwnDb ? 1 : 0); // escape is still safe
                sb.Append(valueSql);
                // sb.Append($"(@folder{i}, @version{i}),");
                // expando.Add(new KeyValuePair<string, object>($"folder{i}", id.FolderName));
                // expando.Add(new KeyValuePair<string, object>($"version{i}", id.Version));
                // SQL logic error: too many SQL variables
            }

            sb.Remove(sb.Length - 1, 1);
            var sql = $@"
DROP TABLE IF EXISTS tmp_table;
CREATE TEMPORARY TABLE tmp_table (
    folder  NVARCHAR (255),
    version NVARCHAR (255),
    ownDb NVARCHAR (255) 
);
INSERT INTO tmp_table (folder, version, ownDb)
                      VALUES {sb};
SELECT *
  FROM beatmap
       INNER JOIN
       tmp_table ON beatmap.folderName = tmp_table.folder AND 
                    beatmap.version = tmp_table.version AND 
                    beatmap.own = tmp_table.ownDb;
";

            return op.ThreadedProvider.GetDbConnection().Query<Beatmap>(sql).ToList();
        }

        // todo: to be optimized
        public static async Task SyncMapsFromHoLLyAsync(this AppDbOperator op, IEnumerable<BeatmapEntry> entry, bool addOnly)
        {
            if (addOnly)
            {
                await Task.Run(() =>
                {
                    var dbMaps = op.ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP, ("own", false));
                    var newList = entry.Select(BeatmapExtension.ParseFromHolly);
                    var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                    AddNewMaps(op, except);
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    RemoveSyncedAll(op);

                    var osuMaps = entry.Select(BeatmapExtension.ParseFromHolly);
                    AddNewMaps(op, osuMaps);
                });
            }
        }

        public static void AddNewMaps(this AppDbOperator op, IEnumerable<Beatmap> beatmaps)
        {
            op.ThreadedProvider.InsertArray(TABLE_BEATMAP, beatmaps.Select(k => new Dictionary<string, object>
            {
                ["id"] = k.Id,
                ["artist"] = k.Artist,
                ["artistU"] = k.ArtistUnicode,
                ["title"] = k.Title,
                ["titleU"] = k.TitleUnicode,
                ["creator"] = k.Creator,
                ["version"] = k.Version,
                ["fileName"] = k.BeatmapFileName,
                ["lastModified"] = k.LastModifiedTime,
                ["diffSrStd"] = k.DiffSrNoneStandard,
                ["diffSrTaiko"] = k.DiffSrNoneTaiko,
                ["diffSrCtb"] = k.DiffSrNoneCtB,
                ["diffSrMania"] = k.DiffSrNoneMania,
                ["drainTime"] = k.DrainTimeSeconds,
                ["totalTime"] = k.TotalTime,
                ["audioPreview"] = k.AudioPreviewTime,
                ["beatmapId"] = k.BeatmapId,
                ["beatmapSetId"] = k.BeatmapSetId,
                ["gameMode"] = k.GameMode,
                ["source"] = k.SongSource,
                ["tags"] = k.SongTags,
                ["folderName"] = k.FolderName,
                ["audioName"] = k.AudioFileName,
                ["own"] = k.InOwnDb,
            }).ToList());
        }
        public static void AddNewMaps(this AppDbOperator op, params Beatmap[] beatmaps)
        {
            AddNewMaps(op, (IEnumerable<Beatmap>)beatmaps);
        }

        public static void RemoveLocalAll(this AppDbOperator op)
        {
            op.ThreadedProvider.Delete(TABLE_BEATMAP, ("own", true));
        }

        public static void RemoveSyncedAll(this AppDbOperator op)
        {
            op.ThreadedProvider.Delete(TABLE_BEATMAP, ("own", false));
        }

        private static string GetKeywordQueryAndArgs(string keywordStr, ref DynamicParameters args)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
            {
                return "1=1";
            }

            var keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (args == null) args = new DynamicParameters();

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

                args.Add($"keyword{i}", $"%{keyword}%");
                if (i != keywords.Length - 1)
                {
                    sb.AppendLine(" AND ");
                }
            }

            return sb.ToString();
        }

        private static string GetOrderAndTakeQueryAndArgs(BeatmapSortMode beatmapSortMode, int startIndex, int count)
        {
            var sb = new StringBuilder();
            switch (beatmapSortMode)
            {
                case BeatmapSortMode.Title:
                    sb.AppendLine(" ORDER BY titleU, title ");
                    break;
                default:
                case BeatmapSortMode.Artist:
                    sb.AppendLine(" ORDER BY artistU, artist ");
                    break;
            }

            sb.AppendLine($" LIMIT {startIndex}, {count} "); // no injection

            return sb.ToString();
        }
    }

    public enum TimeSortMode
    {
        PlayTime, AddTime
    }
}