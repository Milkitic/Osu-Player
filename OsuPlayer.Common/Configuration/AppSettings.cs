using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Milky.OsuPlayer.Common.Configuration
{
    public class AppSettings
    {
        public AppSettings()
        {
            if (Default != null)
            {
                return;
            }

            Default = this;
        }

        public VolumeControl Volume { get; set; } = new VolumeControl();
        public GeneralControl General { get; set; } = new GeneralControl();
        public InterfaceControl Interface { get; set; } = new InterfaceControl();
        public PlayControl Play { get; set; } = new PlayControl();
        [JsonProperty("hot_keys")]
        public List<HotKey> HotKeys { get; set; } = new List<HotKey>();
        public LyricControl Lyric { get; set; } = new LyricControl();
        public ExportControl Export { get; set; } = new ExportControl();
        public HashSet<MapIdentity> CurrentList { get; set; } = new HashSet<MapIdentity>();
        public MapIdentity? CurrentMap { get; set; }
        public DateTime? LastUpdateCheck { get; set; } = null;
        public string IgnoredVer { get; set; } = null;

        public static AppSettings Default { get; private set; }

        public DateTime LastTimeScanOsuDb { get; set; }

        public static void SaveDefault()
        {
            ConcurrentFile.WriteAllText(Domain.ConfigFile,
                JsonConvert.SerializeObject(Default, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    }
                )
            );
        }

        public static void Load(AppSettings config)
        {
            Default = config;
        }

        public static void LoadNew()
        {
            Load(new AppSettings());
        }

        public static void CreateNewConfig()
        {
            LoadNew();
            SaveDefault();
        }
    }
}
