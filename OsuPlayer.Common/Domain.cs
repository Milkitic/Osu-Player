using Milky.OsuPlayer.Common.Configuration;
using System;
using System.IO;

namespace Milky.OsuPlayer.Common
{
    public static class Domain
    {
        static Domain()
        {
            Type t = typeof(Domain);
            var infos = t.GetProperties();
            foreach (var item in infos)
            {
                if (!item.Name.EndsWith("Path")) continue;
                try
                {
                    string path = (string)item.GetValue(null, null);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    Console.WriteLine(@"未创建：" + item.Name);
                }
            }
        }

        public static string CurrentPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ConfigFile => Path.Combine(CurrentPath, "config.json");

        public static string CachePath => Path.Combine(CurrentPath, "_Cache");
        public static string LyricCachePath => Path.Combine(CachePath, "_Lyric");
        public static string ThumbCachePath => Path.Combine(CachePath, "_Thumbs");

        public static string DefaultPath => Path.Combine(CurrentPath, "Default");
        public static string ExternalPath => Path.Combine(CurrentPath, "External");
        public static string MusicPath => Path.Combine(CurrentPath, "Music");
        public static string BackgroundPath => Path.Combine(CurrentPath, "Background");
        public static string ResourcePath => Path.Combine(CurrentPath, "Resources");
        public static string OwnSongsPath => Path.Combine(CurrentPath, "Songs");
        public static string PluginPath => Path.Combine(ExternalPath, "Plugins");

        public static string CustomSongPath => AppSettings.Default == null ? null : new FileInfo(AppSettings.Default.General.CustomSongsPath).FullName;
        public static string OsuPath => AppSettings.Default == null ? null : new FileInfo(AppSettings.Default.General.DbPath).Directory.FullName;
        public static string OsuSongPath => OsuPath == null ? null : Path.Combine(OsuPath, "Songs");
    }
}
