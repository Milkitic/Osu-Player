using System;
using System.IO;

namespace Milkitic.OsuPlayer
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

        public static string OsuPath => new FileInfo(App.Config.DbPath).Directory.FullName;
        public static string OsuSongPath => Path.Combine(OsuPath, "Songs");
    }
}
