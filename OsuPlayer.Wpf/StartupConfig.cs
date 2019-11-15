﻿using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.WpfApi;
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

        private static void InitLocalDb()
        {
            AppDbOperator.ValidateDb();
            BeatmapDbOperator.ValidateDb();

            var appDbOperator = new AppDbOperator();
            var defCol = appDbOperator.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) appDbOperator.AddCollection("最喜爱的", true);
        }
    }
}