using System;
using System.IO;
using Milky.OsuPlayer.Common.Configuration;

namespace Milky.OsuPlayer.Common
{
    public static class Domain
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
                    if (path == null) continue;
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "未创建：{dirName}", item.Name);
                }
            }
        }

        public static string CurrentPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ConfigFile => Path.Combine(CurrentPath, "config.json");

        public static string CachePath => Path.Combine(CurrentPath, "_cache");
        public static string LyricCachePath => Path.Combine(CachePath, "_lyric");
        public static string ThumbCachePath => Path.Combine(CachePath, "_thumbs");

        public static string DefaultPath => Path.Combine(CurrentPath, "default");
        public static string ExtensionPath => Path.Combine(CurrentPath, "extensions");
        public static string MusicPath => Path.Combine(CurrentPath, "music");
        public static string BackgroundPath => Path.Combine(CurrentPath, "background");
        public static string LangPath => Path.Combine(CurrentPath, "lang");
        public static string ResourcePath => Path.Combine(CurrentPath, "Resources");
        public static string PluginPath => Path.Combine(ExtensionPath, "plugins");

        public static string CustomSongPath
        => AppSettings.Default == null ? null : new FileInfo(AppSettings.Default.General.CustomSongsPath).FullName;
        public static string OsuPath
            => AppSettings.Default == null
                ? null
                : (AppSettings.Default.General.DbPath == null
                    ? null
                    : new FileInfo(AppSettings.Default.General.DbPath).Directory?.FullName);
        public static string OsuSongPath => OsuPath == null ? null : Path.Combine(OsuPath, "Songs");
    }
}
