using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class AppSettings
    {
        public AppSettings()
        {
            if (Current != null)
            {
                return;
            }

            Current = this;
        }

        public VolumeControl Volume { get; set; } = new VolumeControl();
        public GeneralControl General { get; set; } = new GeneralControl();
        public PlayControl Play { get; set; } = new PlayControl();
        public List<HotKey> HotKeys { get; set; } = new List<HotKey>();
        public LyricControl Lyric { get; set; } = new LyricControl();
        public ExportControl Export { get; set; } = new ExportControl();
        public List<MapIdentity> CurrentList { get; set; } = new List<MapIdentity>();
        public string CurrentPath { get; set; }
        public DateTime? LastUpdateCheck { get; set; } = null;
        public string IgnoredVer { get; set; } = null;

        public static AppSettings Current { get; private set; }

        public DateTime LastTimeScanOsuDb { get; set; }

        public static void SaveCurrent()
        {
            ConcurrentFile.WriteAllText(Domain.ConfigFile, JsonConvert.SerializeObject(Current, Formatting.Indented));
        }

        public static void Load(AppSettings config)
        {
            Current = config;
        }

        public static void LoadNew()
        {
            Load(new AppSettings());
        }

        public static void CreateNewConfig()
        {
            LoadNew();
            SaveCurrent();
        }
    }
}
