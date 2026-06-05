using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OsuPlayer.Media.Audio.SoundTouch;

internal static class SoundTouchRuntime
{
    private static string _runtimeRoot = Path.Combine(AppContext.BaseDirectory, "runtimes");

    public static string RuntimeRoot => _runtimeRoot;

    public static void Configure(string runtimeRoot)
    {
        if (string.IsNullOrWhiteSpace(runtimeRoot))
        {
            throw new ArgumentException("SoundTouch runtime root cannot be empty.", nameof(runtimeRoot));
        }

        _runtimeRoot = runtimeRoot;
    }

    internal static void EnsureSupported()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("SoundTouch playback-rate processing is only configured for Windows runtimes.");
        }
    }

    internal static string GetNativeLibraryPath()
    {
        var runtimeFolder = Environment.Is64BitProcess ? "win-x64" : "win-x86";
        return Path.Combine(_runtimeRoot, runtimeFolder, "SoundTouch", "SoundTouch.dll");
    }
}
