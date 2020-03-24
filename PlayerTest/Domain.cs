using System;
using System.IO;

namespace PlayerTest
{
    internal class Domain
    {
        public static string CurrentPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ConfigFile => Path.Combine(CurrentPath, "config.json");

        public static string CachePath => Path.Combine(CurrentPath, "_cache");
        public static string LyricCachePath => Path.Combine(CachePath, "_lyric");
        public static string ThumbCachePath => Path.Combine(CachePath, "_thumbs");

        public static string DefaultPath => Path.Combine(CurrentPath, "default");
        public static string ExternalPath => Path.Combine(CurrentPath, "external");
        public static string MusicPath => Path.Combine(CurrentPath, "music");
        public static string BackgroundPath => Path.Combine(CurrentPath, "background");
        public static string LangPath => Path.Combine(CurrentPath, "lang");
        public static string ResourcePath => Path.Combine(CurrentPath, "Resources");
        public static string PluginPath => Path.Combine(ExternalPath, "plugins");
    }
}