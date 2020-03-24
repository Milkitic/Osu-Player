using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace PlayerTest
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
        public static AppSettings Default { get; private set; }
        public PlaySection Play { get; set; } = new PlaySection();

        public static void SaveDefault()
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
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
