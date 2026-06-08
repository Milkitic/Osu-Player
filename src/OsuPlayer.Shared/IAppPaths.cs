namespace OsuPlayer.Shared;

public interface IAppPaths
{
    string BasePath { get; }
    string ConfigFile { get; }
    string CachePath { get; }
    string ThumbCachePath { get; }
    string LyricCachePath { get; }
    string DefaultPath { get; }
    string ExtensionPath { get; }
    string MusicPath { get; }
    string BackgroundPath { get; }
    string LangPath { get; }
    string ResourcePath { get; }
    string PluginPath { get; }
    string? OsuSongPath { get; }
    string? CustomSongPath { get; }
}