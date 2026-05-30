using Dapper.FluentMap;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Core.Configuration;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Shared;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer
{
    public static class EntryStartup
    {
        public static async Task StartupAsync()
        {
            LogManager.Setup().SetupExtensions(setup =>
                setup.RegisterLayoutRenderer<InvariantCultureLayoutRendererWrapper>("InvariantCulture"));
            if (!LoadConfig())
            {
                Environment.Exit(0);
                return;
            }

#if DEBUG
            //ConsoleManager.Show();
#endif

            await InitLocalDbAsync();

            StyleUtilities.SetAlignment();

            // Keep FFmpeg binaries separated by process architecture to avoid x86/x64 mismatches.
            var ffmpegArchitecture = Environment.Is64BitProcess ? "win-x64" : "win-x86";
            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg", ffmpegArchitecture);
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

        private static async Task InitLocalDbAsync()
        {
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new StoryboardInfoMap());
                config.AddMap(new BeatmapMap());
                config.AddMap(new BeatmapSettingsMap());
                config.AddMap(new CollectionMap());
                config.AddMap(new CollectionRelationMap());
            });

            await OsuPlayerDbContext.InitializeDatabaseAsync();

            var playerData = new PlayerDataService();
            var defCol = await playerData.GetCollectionsAsync();
            var locked = defCol.Where(k => k.LockedBool);
            if (!locked.Any()) await playerData.TryAddCollectionAsync("Favorite", true);
        }
    }
}
