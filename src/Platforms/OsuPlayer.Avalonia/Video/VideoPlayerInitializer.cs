using System;
using Avalonia.VlcVideoPlayer;

namespace OsuPlayer.Video;

/// <summary>
/// 初始化 FFmpegVideoPlayer.Avalonia(实际为 VLC 后端)的 FFmpeg/VLC 原生库加载。
/// 替代 WPF FFME.Windows 的 FFmpeg 自动加载逻辑。
/// </summary>
public static class VideoPlayerInitializer
{
    public static void Initialize()
    {
        try
        {
            FFmpegInitializer.Initialize();
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn(ex,
                "FFmpeg init failed; video playback will be disabled");
        }
    }
}
