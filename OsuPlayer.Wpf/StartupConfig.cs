using Dapper.FluentMap;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Presentation;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
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

            StyleUtilities.SetAlignment();

            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
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
                    AppSettings.Load(JsonConvert.DeserializeObject<AppSettings>(content,
                            new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.Auto
                            }
                        )
                    );
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show("载入配置文件时失败，用默认配置覆盖继续打开吗？" + Environment.NewLine + e.Message,
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

        private static void InitLocalDb()
        {
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new StoryboardInfoMap());
                config.AddMap(new BeatmapMap());
                config.AddMap(new MapInfoMap());
            });

            AppDbOperator.ValidateDb();

            var appDbOperator = new AppDbOperator();
            var defCol = appDbOperator.GetCollections();
            var locked = defCol.Where(k => k.LockedBool);
            if (!locked.Any()) appDbOperator.AddCollection("最喜爱的", true);
        }
    }
}