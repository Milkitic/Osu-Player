using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
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

namespace Milky.OsuPlayer.Common.Data
{
    public class BeatmapDbOperator
    {
        private const string TABLE_BEATMAP = "beatmap";

        static BeatmapDbOperator()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        private static ReadOnlyDictionary<string, string> _creationMapping =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>()
                {
                    ["beatmap"] = @"
CREATE TABLE beatmap (
    id            UNIQUEIDENTIFIER      NOT NULL,
    artist        NVARCHAR (2147483647),
    artistU       NVARCHAR (2147483647),
    title         NVARCHAR (2147483647),
    titleU        NVARCHAR (2147483647),
    creator       NVARCHAR (2147483647),
    version       NVARCHAR (2147483647),
    fileName      NVARCHAR (2147483647),
    lastModified  DATETIME              NOT NULL,
    diffSrStd     FLOAT                 NOT NULL,
    diffSrTaiko   FLOAT                 NOT NULL,
    diffSrCtb     FLOAT                 NOT NULL,
    diffSrMania   FLOAT                 NOT NULL,
    drainTime     INT                   NOT NULL,
    totalTime     INT                   NOT NULL,
    audioPreview  INT                   NOT NULL,
    beatmapId     INT                   NOT NULL,
    beatmapSetId  INT                   NOT NULL,
    gameMode      INT                   NOT NULL,
    source        NVARCHAR (2147483647),
    tags          NVARCHAR (2147483647),
    folderName    NVARCHAR (2147483647),
    audioName     NVARCHAR (2147483647),
    own           BIT                   NOT NULL,
    PRIMARY KEY (
        id
    )
);
PRAGMA case_sensitive_like=false;"
                });

        private static ThreadLocal<SQLiteProvider> _provider = new ThreadLocal<SQLiteProvider>(() =>
            (SQLiteProvider)new SQLiteProvider().ConfigureConnectionString("data source=beatmap.db"));

        private static SQLiteProvider ThreadedProvider => _provider.Value;

        public static void ValidateDb()
        {
            var dbFile = Path.Combine(Domain.CurrentPath, "player.db");
            if (!File.Exists(dbFile))
            {
                File.WriteAllText(dbFile, "");
            }

            var tables = ThreadedProvider.GetAllTables();

            foreach (var pair in _creationMapping)
            {
                if (tables.Contains(pair.Key)) continue;
                try
                {
                    ThreadedProvider.GetDbConnection().Execute(pair.Value);
                }
                catch (Exception exc)
                {
                    throw new Exception($"创建表`{pair}`失败", exc);
                }
            }
        }

        public List<Beatmap> SearchBeatmapByOptions(string searchText, SortMode sortMode, int startIndex, int count)
        {
            var expando = new DynamicParameters();
            var command = " SELECT * FROM beatmap WHERE ";
            var keywordSql = GetKeywordQueryAndArgs(searchText, ref expando);
            var sort = GetOrderAndTakeQueryAndArgs(sortMode, startIndex, count);
            var sw = Stopwatch.StartNew();
            try
            {
                return ThreadedProvider.GetDbConnection().Query<Beatmap>(command + keywordSql + sort, expando).ToList();
            }
            finally
            {
                Console.WriteLine($"query: {sw.ElapsedMilliseconds}");
                sw.Stop();
            }
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
                var valueSql = string.Format("('{0}', '{1}'),", id.FolderName.Replace(@"'", @"''"),
                    id.Version.Replace(@"'", @"''")); // escape is still safe
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

                    AddNewMaps(except);
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    RemoveSyncedAll();

                    var osuMaps = entry.Select(Beatmap.ParseFromHolly);
                    AddNewMaps(osuMaps);
                });
            }
        }

        public void AddNewMaps(IEnumerable<Beatmap> beatmaps)
        {
            ThreadedProvider.InsertArray(TABLE_BEATMAP, beatmaps.Select(k => new Dictionary<string, object>
            {
                ["id"] = k.Id,
                ["artist"] = k.Artist,
                ["artistU"] = k.ArtistUnicode,
                ["title"] = k.Title,
                ["titleU"] = k.TitleUnicode,
                ["creator"] = k.Creator,
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
                ["own"] = k.InOwnFolder,
            }).ToList());
        }
        public void AddNewMaps(params Beatmap[] beatmaps)
        {
            AddNewMaps((IEnumerable<Beatmap>)beatmaps);
        }

        public void RemoveLocalAll()
        {
            ThreadedProvider.Delete(TABLE_BEATMAP, ("own", true));
        }

        public void RemoveSyncedAll()
        {
            ThreadedProvider.Delete(TABLE_BEATMAP, ("own", false));
        }

        private string GetKeywordQueryAndArgs(string keywordStr, ref DynamicParameters args)
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