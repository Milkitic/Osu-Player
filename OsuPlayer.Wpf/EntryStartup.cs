using Dapper.FluentMap;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Shared;
using Newtonsoft.Json;
using NLog.Config;
using System;
using System.IO;
using System.Linq;
using System.Windows;

#if !DEBUG
using Sentry;
#endif

namespace Milky.OsuPlayer
{
    public static class EntryStartup
    {
        public static void Startup()
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("InvariantCulture", typeof(InvariantCultureLayoutRendererWrapper));
            if (!LoadConfig())
            {
                Environment.Exit(0);
                return;
            }

#if DEBUG
            ConsoleManager.Show();
#endif

#if !DEBUG
            SentrySdk.Init("https://1fe13baa86284da5a0a70efa9750650e:fcbd468d43f94fb1b43af424517ec00b@sentry.io/1412154");
#endif

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
                catch (JsonException ex)
                {
                    var result = MessageBox.Show("载入配置文件时失败，用默认配置覆盖继续打开吗？" + Environment.NewLine + ex.Message,
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
                config.AddMap(new BeatmapSettingsMap());
                config.AddMap(new CollectionMap());
                config.AddMap(new CollectionRelationMap());
            });

            AppDbOperator.ValidateDb();

            var appDbOperator = new AppDbOperator();
            var defCol = appDbOperator.GetCollections();
            var locked = defCol.Where(k => k.LockedBool);
            if (!locked.Any()) appDbOperator.AddCollection("最喜爱的", true);
        }
    }
}