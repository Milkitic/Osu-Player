using System;
using Avalonia.VlcVideoPlayer;
using OsuPlayer.Shared;

namespace OsuPlayer.Avalonia.Video;

/// <summary>
/// 初始化 FFmpegVideoPlayer.Avalonia 的 FFmpeg 原生库加载。
/// 替代 WPF FFME.Windows 的 FFmpeg 自动加载逻辑。
/// </summary>
public static class VideoPlayerInitializer
{
    public static void Initialize()
    {
        try
        {
            // 优先使用 osu!player 提供的本地 ffmpeg DLL
            var ffmpegDir = System.IO.Path.Combine(
                AppPaths.Current.PluginPath, "ffmpeg",
                Environment.Is64BitProcess ? "win-x64" : "win-x86");
            if (System.IO.Directory.Exists(ffmpegDir))
            {
                FFmpegInitializer.Initialize(ffmpegDir);
                return;
            }
        }
        catch
        {
            // 继续尝试 bundled
        }
        try
        {
            FFmpegInitializer.Initialize();
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn(ex, "FFmpeg init failed; video playback will be disabled");
        }
    }
}
