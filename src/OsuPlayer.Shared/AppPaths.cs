using System;
using System.IO;

namespace OsuPlayer.Shared;

public sealed class AppPaths : IAppPaths
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public string BasePath { get; }
    public string? OsuSongPath { get; }
    public string? CustomSongPath { get; }

    public AppPaths(string? osuSongPath = null, string? customSongPath = null)
        : this(AppDomain.CurrentDomain.BaseDirectory, osuSongPath, customSongPath) { }

    public AppPaths(string basePath, string? osuSongPath, string? customSongPath)
    {
        BasePath = basePath;
        OsuSongPath = osuSongPath;
        CustomSongPath = customSongPath;
        Current = this;
        EnsureDirectoriesExist();
    }

    public string ConfigFile => Path.Combine(BasePath, "config.json");
    public string CachePath => Path.Combine(BasePath, "_cache");
    public string ThumbCachePath => Path.Combine(BasePath, "_cache", "_thumbs");
    public string LyricCachePath => Path.Combine(BasePath, "_cache", "_lyric");
    public string DefaultPath => Path.Combine(BasePath, "default");
    public string ExtensionPath => Path.Combine(BasePath, "extensions");
    public string MusicPath => Path.Combine(BasePath, "music");
    public string BackgroundPath => Path.Combine(BasePath, "background");
    public string LangPath => Path.Combine(BasePath, "lang");
    public string ResourcePath => Path.Combine(BasePath, "Resources");
    public string PluginPath => Path.Combine(BasePath, "extensions", "plugins");

    public static AppPaths Current { get; set; } = new AppPaths();

    private void EnsureDirectoriesExist()
    {
        var paths = new[]
        {
            CachePath, ThumbCachePath, LyricCachePath, DefaultPath,
            ExtensionPath, MusicPath, BackgroundPath, LangPath, ResourcePath, PluginPath
        };
        foreach (var path in paths)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (System.Exception ex)
            {
                Logger.Warn(ex, "未创建：{dirName}", path);
            }
        }
    }
}