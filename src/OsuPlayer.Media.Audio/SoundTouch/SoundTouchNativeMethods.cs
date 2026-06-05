using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Milky.OsuPlayer.Media.Audio.SoundTouch;

internal static class SoundTouchNativeMethods
{
    private const string SoundTouchDllName = "SoundTouch.dll";

    static SoundTouchNativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(SoundTouchNativeMethods).Assembly, ResolveLibrary);
    }

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_createInstance", CallingConvention = CallingConvention.Cdecl)]
    internal static extern SoundTouchHandle CreateInstance();

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_destroyInstance", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void DestroyInstance(IntPtr handle);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_getVersionString2", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void GetVersionString([Out] byte[] versionString, int bufferSize);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_setPitchOctaves", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SetPitchOctaves(SoundTouchHandle handle, float pitchOctaves);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_setSampleRate", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SetSampleRate(SoundTouchHandle handle, uint sampleRate);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_setChannels", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SetChannels(SoundTouchHandle handle, uint channels);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_putSamples", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void PutSamples(SoundTouchHandle handle, [In] float[] samples, int numSamples);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_receiveSamples", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint ReceiveSamples(SoundTouchHandle handle, [Out] float[] outBuffer, uint maxSamples);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_isEmpty", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int IsEmpty(SoundTouchHandle handle);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_numSamples", CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint NumSamples(SoundTouchHandle handle);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_numUnprocessedSamples", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int NumUnprocessedSamples(SoundTouchHandle handle);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_flush", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Flush(SoundTouchHandle handle);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_clear", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Clear(SoundTouchHandle handle);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_setRate", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SetRate(SoundTouchHandle handle, float newRate);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_setTempo", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SetTempo(SoundTouchHandle handle, float newTempo);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_setSetting", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int SetSetting(SoundTouchHandle handle, SoundTouchSettings settingId, int value);

    [DllImport(SoundTouchDllName, EntryPoint = "soundtouch_getSetting", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int GetSetting(SoundTouchHandle handle, SoundTouchSettings settingId);

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, SoundTouchDllName, StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        SoundTouchRuntime.EnsureSupported();
        var nativeLibraryPath = SoundTouchRuntime.GetNativeLibraryPath();
        return NativeLibrary.TryLoad(nativeLibraryPath, out var handle) ? handle : IntPtr.Zero;
    }
}
