using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Milky.OsuPlayer
{
    public static class StartupConfig
    {
        public static void Startup()
        {
            if (!LoadConfig())
                Environment.Exit(0);

            InitLocalDb();

            //EventRedirectHandle.Redirect();
            StyleUtilities.SetAlignment();

            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
            //SetDbPath();
        }

        private static bool LoadConfig()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                AppSettings.CreateNewConfig();
            }
            else
            {
                try
                {
                    var content = ConcurrentFile.ReadAllText(file);
                    AppSettings.Load(JsonConvert.DeserializeObject<AppSettings>(content));
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show(@"载入配置文件时失败，用默认配置覆盖继续打开吗？\r\n" + e.Message,
                        "Osu Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        AppSettings.CreateNewConfig();
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private static readonly Dictionary<string, string> CreateTableMapping = new Dictionary<string, string>()
        {
            ["collection"] = @"
CREATE TABLE collection (
    [id]          NVARCHAR (128)        NOT NULL,
    [name]        NVARCHAR (2147483647) NOT NULL,
    [locked]      INT                   NOT NULL,
    [index]       INT                   NOT NULL,
    [imagePath]   NVARCHAR (2147483647),
    [description] NVARCHAR (2147483647),
    [createTime]  DATETIME              NOT NULL,
    PRIMARY KEY (
        id
    )
);",
            ["collection_relation"] = @"
CREATE TABLE collection_relation (
    [id]           NVARCHAR (128)        NOT NULL,
    [collectionId] NVARCHAR (2147483647) NOT NULL,
    [mapId]        NVARCHAR (2147483647) NOT NULL,
    [addTime]      DATETIME,
    PRIMARY KEY (
        id
    )
);",
            ["map_info"] = @"
CREATE TABLE map_info (
    [id]           NVARCHAR (128)        NOT NULL,
    [version]      NVARCHAR (2147483647) NOT NULL,
    [folder]       NVARCHAR (2147483647) NOT NULL,
    [offset]       INT                   NOT NULL,
    [lastPlayTime] DATETIME,
    [exportFile]   NVARCHAR (2147483647),
    PRIMARY KEY (
        id
    )
);"
        };
        private static void InitLocalDb()
        {
            var dbFile = Path.Combine(Domain.CurrentPath, "player.db");
            if (!File.Exists(dbFile))
            {
                using (var conn = new SQLiteConnection("data source=player.db"))
                {
                    File.WriteAllText(dbFile, "");
                    CreateTables(conn);
                }
            }

            var appDbOperator = new AppDbOperator();
            var defCol = appDbOperator.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) appDbOperator.AddCollection("最喜爱的", true);
        }

        private static void CreateTables(SQLiteConnection conn)
        {
            foreach (var pair in CreateTableMapping)
            {
                try
                {
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();
                    var command = conn.CreateCommand();
                    command.CommandText = pair.Value;
                    command.ExecuteNonQuery();
                }
                catch (Exception exc)
                {
                    throw new Exception($"创建表`{pair}`失败", exc);
                }
                finally
                {
                    conn?.Close();
                }
            }
        }

        private static void SetDbPath()
        {
            string dbPath = AppSettings.Default.General.DbPath;
            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            {
                var osuProcess = Process.GetProcesses().Where(x => x.ProcessName == "osu!").ToArray();
                if (osuProcess.Length == 1)
                {
                    var di = new FileInfo(osuProcess[0].MainModule.FileName).Directory;
                    if (di != null && di.Exists)
                        dbPath = Path.Combine(di.FullName, "osu!.db");
                }

                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                {
                    var result = Util.BrowseDb(out var chosedPath);
                    if (!result.HasValue || !result.Value)
                    {
                        MessageBox.Show(@"你尚未初始化osu!db，因此部分功能将不可用。", "Osu Player", MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    if (!File.Exists(chosedPath))
                    {
                        MessageBox.Show(@"指定文件不存在。", "Osu Player", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    dbPath = chosedPath;
                }

                //if (dbPath == null) return;
                AppSettings.Default.General.DbPath = dbPath;
            }
        }
    }
}