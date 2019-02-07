using System;
using System.IO;

namespace Milky.OsuPlayer
{
    internal static class Domain
    {
        public static string CurrentPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ConfigFile => Path.Combine(CurrentPath, "config.json");

        public static string CachePath => Path.Combine(CurrentPath, "_Cache");
        public static string LyricCachePath => Path.Combine(CachePath, "_Lyric");

        public static string DefaultPath => Path.Combine(CurrentPath, "Default");
        public static string ResourcePath => Path.Combine(CurrentPath, "Resource");
        public static string MusicPath => Path.Combine(CurrentPath, "Music");
        public static string BackgroundPath => Path.Combine(CurrentPath, "Background");
        public static string PluginPath => Path.Combine(CurrentPath, "Plugins");

        public static string OsuPath =>
            App.UseDbMode ? new FileInfo(App.Config.General.DbPath).Directory.FullName : null;
        public static string OsuSongPath => App.UseDbMode ? Path.Combine(OsuPath, "Songs") : null;
    }
}
