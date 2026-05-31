using FFmpeg.AutoGen;
using System;
using System.Runtime.InteropServices;

namespace Milky.OsuPlayer
{
    internal sealed class FFmpegWindowsFunctionResolver : FunctionResolverBase
    {
        private const int LoadLibrarySearchDllLoadDir = 0x00000100;
        private const int LoadLibrarySearchDefaultDirs = 0x00001000;

        protected override string GetNativeLibraryName(string libraryName, int version) => $"{libraryName}-{version}.dll";

        protected override IntPtr LoadNativeLibrary(string libraryName)
        {
            // FFmpeg's external codec DLLs live beside the loaded libav*.dll files.
            var handle = LoadLibraryEx(
                libraryName,
                IntPtr.Zero,
                LoadLibrarySearchDllLoadDir | LoadLibrarySearchDefaultDirs);

            return handle != IntPtr.Zero ? handle : LoadLibrary(libraryName);
        }

        protected override IntPtr FindFunctionPointer(IntPtr nativeLibraryHandle, string functionName)
            => GetProcAddress(nativeLibraryHandle, functionName);

        [DllImport("kernel32", BestFitMapping = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, int dwFlags);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);
    }
}
