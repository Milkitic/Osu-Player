using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Milky.OsuPlayer.Common.Data.Dapper;
using Milky.OsuPlayer.Common.Data.Dapper.Provider;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap.MetaData;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.Common.Data.EF
{
    public class BeatmapDbOperator
    {
        private const string TABLE_BEATMAP = "beatmap";

        static BeatmapDbOperator()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private static ThreadLocal<SQLiteProvider> _provider = new ThreadLocal<SQLiteProvider>(() =>
            (SQLiteProvider)new SQLiteProvider().ConfigureConnectionString("data source=beatmap.db"));

        private static SQLiteProvider ThreadedProvider => _provider.Value;

        public List<Beatmap> SearchBeatmapByOptions(string searchText, SortMode sortMode, int startIndex, int count)
        {
            var expando = new ExpandoObject();
            var command = " SELECT * FROM beatmap WHERE ";
            var keywordSql = GetKeywordQueryAndArgs(searchText, ref expando);
            var sort = GetOrderAndTakeQueryAndArgs(sortMode, startIndex, count);

            return ThreadedProvider.GetDbConnection().Query<Beatmap>(command + keywordSql + sort, expando).ToList();
        }

        public List<Beatmap> GetAllBeatmaps()
        {
            return ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP).ToList();
        }

        public Beatmap GetBeatmapByIdentifiable(IMapIdentifiable id)
        {
            return ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP,
                new Where[]
                {
                    ("version", id.Version),
                    ("folderName", id.FolderName)
                },
                count: 1).FirstOrDefault();
        }

        public List<Beatmap> GetBeatmapsByMapInfo(List<MapInfo> reqList, TimeSortMode sortMode)
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
            return ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP, ("folderName", folder)).ToList();
        }

        public List<Beatmap> GetBeatmapsByIdentifiable<T>(List<T> reqList)
            where T : IMapIdentifiable
        {
            if (reqList.Count < 1) return new List<Beatmap>();

            var args = new ExpandoObject();
            var expando = (ICollection<KeyValuePair<string, object>>)args;

            var sb = new StringBuilder();
            for (var i = 0; i < reqList.Count; i++)
            {
                var id = reqList[i];
                sb.AppendLine($"(@folder{i}, @version{i})");
                expando.Add(new KeyValuePair<string, object>($"folder{i}", id.FolderName));
                expando.Add(new KeyValuePair<string, object>($"version{i}", id.Version));
            }

            var sql = $@"
DROP TABLE IF EXISTS tmp_table;
CREATE TEMPORARY TABLE tmp_table (
    folder  NVARCHAR (255),
    version NVARCHAR (255) 
);
INSERT INTO tmp_table (folder, version)
                      VALUES {sb};
SELECT *
  FROM beatmap
       INNER JOIN
       tmp_table ON beatmap.folderName = tmp_table.folder AND 
                    beatmap.version = tmp_table.version;
";

            return ThreadedProvider.GetDbConnection().Query<Beatmap>(sql, args).ToList();
        }

        // todo: to be optimized
        public async Task SyncMapsFromHoLLyAsync(IEnumerable<BeatmapEntry> entry, bool addOnly)
        {
            if (addOnly)
            {
                await Task.Run(() =>
                {
                    var dbMaps = ThreadedProvider.Query<Beatmap>(TABLE_BEATMAP, ("own", false));
                    var newList = entry.Select(Beatmap.ParseFromHolly);
                    var except = newList.Except(dbMaps, new Beatmap.Comparer(true));

                    foreach (var beatmap in except) // todo: no!!!!!!!!!
                    {
                        AddNewMap(beatmap);
                    }
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    RemoveSyncedAll();

                    var osuMaps = entry.Select(Beatmap.ParseFromHolly);
                    foreach (var beatmap in osuMaps) // todo: no!!!!!!!!!
                    {
                        AddNewMap(beatmap);
                    }
                });
            }
        }

        public void AddNewMap(Beatmap beatmap)
        {
            ThreadedProvider.Insert(TABLE_BEATMAP, new Dictionary<string, object>
            {
                ["id"] = beatmap.Id,
                ["artist"] = beatmap.Artist,
                ["artistU"] = beatmap.ArtistUnicode,
                ["title"] = beatmap.Title,
                ["titleU"] = beatmap.TitleUnicode,
                ["creator"] = beatmap.Creator,
                ["fileName"] = beatmap.BeatmapFileName,
                ["lastModified"] = beatmap.LastModifiedTime,
                ["diffSrStd"] = beatmap.DiffSrNoneStandard,
                ["diffSrTaiko"] = beatmap.DiffSrNoneTaiko,
                ["diffSrCtb"] = beatmap.DiffSrNoneCtB,
                ["diffSrMania"] = beatmap.DiffSrNoneMania,
                ["drainTime"] = beatmap.DrainTimeSeconds,
                ["totalTime"] = beatmap.TotalTime,
                ["audioPreview"] = beatmap.AudioPreviewTime,
                ["beatmapId"] = beatmap.BeatmapId,
                ["beatmapSetId"] = beatmap.BeatmapSetId,
                ["gameMode"] = beatmap.GameMode,
                ["source"] = beatmap.SongSource,
                ["tags"] = beatmap.SongTags,
                ["folderName"] = beatmap.FolderName,
                ["audioName"] = beatmap.AudioFileName,
                ["own"] = beatmap.InOwnFolder,

            });
        }

        public void RemoveLocalAll()
        {
            ThreadedProvider.Delete(TABLE_BEATMAP, ("own", true));
        }

        public void RemoveSyncedAll()
        {
            ThreadedProvider.Delete(TABLE_BEATMAP, ("own", false));
        }

        private string GetKeywordQueryAndArgs(string keywordStr, ref ExpandoObject args)
        {
            if (string.IsNullOrWhiteSpace(keywordStr))
            {
                return "1=1";
            }

            var keywords = keywordStr.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (args == null) args = new ExpandoObject();
            var expando = (ICollection<KeyValuePair<string, object>>)args;

            var sb = new StringBuilder();
            for (var i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i];
                var postfix = $" like @keyword{i} COLLATE NOCASE ";
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

                expando.Add(new KeyValuePair<string, object>($"keyword{i}", $"%{keyword}%"));
                if (i != keywords.Length - 1)
                {
                    sb.AppendLine(" AND ");
                }
            }

            return sb.ToString();
        }

        private string GetOrderAndTakeQueryAndArgs(SortMode sortMode, int startIndex, int count)
        {
            var sb = new StringBuilder();
            switch (sortMode)
            {
                case SortMode.Title:
                    sb.AppendLine(" ORDER BY titleU, title ");
                    break;
                default:
                case SortMode.Artist:
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