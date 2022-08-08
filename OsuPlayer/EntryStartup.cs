using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Milki.Extensions.MixPlayer;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Common.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Shared;
using Milki.OsuPlayer.Wpf;
using Newtonsoft.Json;
using NLog.Config;

namespace Milki.OsuPlayer
{
    public static class EntryStartup
    {
        public static async Task StartupAsync()
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("InvariantCulture", typeof(InvariantCultureLayoutRendererWrapper));
            if (!LoadConfig())
            {
                Environment.Exit(0);
                return;
            }

            InitMixPlayerConfig();

#if DEBUG
            //ConsoleManager.Show();
#endif

            await InitLocalDb();

            StyleUtilities.SetAlignment();

            //https://ffmpeg.zeranoe.com/builds/win32/shared/ffmpeg-4.2.1-win32-shared.zip
            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(Domain.PluginPath, "ffmpeg");
        }

        private static void InitMixPlayerConfig()
        {
            var playSection = AppSettings.Default.Play;
            var configuration = Configuration.Instance;
            configuration.CacheDir = Domain.CachePath;
            configuration.DefaultDir = Domain.DefaultPath;
            configuration.PlaybackRate = playSection.PlaybackRate;
            configuration.KeepTune = playSection.PlayUseTempo;
            configuration.GeneralOffset = (uint)playSection.GeneralOffset;
            AppSettings.Default.Play.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(AppSettings.Play.PlaybackRate))
                    configuration.PlaybackRate = playSection.PlaybackRate;
                else if (e.PropertyName == nameof(AppSettings.Play.PlayUseTempo))
                    configuration.KeepTune = playSection.PlayUseTempo;
                else if (e.PropertyName == nameof(AppSettings.Play.GeneralOffset))
                    configuration.GeneralOffset = (uint)playSection.GeneralOffset;
            };
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

        private static async Task InitLocalDb()
        {
            await using var dbContext = new ApplicationDbContext();
            dbContext.Database.Migrate();
        }
    }
}