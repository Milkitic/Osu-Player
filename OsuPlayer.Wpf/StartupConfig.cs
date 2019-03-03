using System;
using System.IO;
using System.Linq;
using System.Windows;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;
using Newtonsoft.Json;

namespace Milky.OsuPlayer
{
    public static class StartupConfig
    {
        public static void Startup()
        {
            if (!LoadConfig())
                Environment.Exit(0);

            InitLocalDb();

            EventRedirectHandle.Redirect();
            StyleUtilities.SetAlignment();
        }

        private static bool LoadConfig()
        {
            var file = Domain.ConfigFile;
            if (!File.Exists(file))
            {
                PlayerConfig.CreateNewConfig();
            }
            else
            {
                try
                {
                    PlayerConfig.Load(JsonConvert.DeserializeObject<PlayerConfig>(ConcurrentFile.ReadAllText(file)));
                }
                catch (JsonException e)
                {
                    var result = MessageBox.Show(@"载入配置文件时失败，用默认配置覆盖继续打开吗？\r\n" + e.Message,
                        "Osu Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        PlayerConfig.CreateNewConfig();
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private static void InitLocalDb()
        {
            var defCol = DbOperate.GetCollections().Where(k => k.Locked);
            if (!defCol.Any()) DbOperate.AddCollection("最喜爱的", true);
        }
    }
}