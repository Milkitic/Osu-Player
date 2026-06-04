using System;
using System.Text;

namespace Milky.OsuPlayer.Media.Audio.SoundTouch;

internal sealed class SoundTouchProcessor : IDisposable
{
    private readonly SoundTouchHandle _handle;
    private string? _versionString;

    public SoundTouchProcessor()
    {
        SoundTouchRuntime.EnsureSupported();

        try
        {
            _handle = SoundTouchNativeMethods.CreateInstance();
        }
        catch (DllNotFoundException ex)
        {
            throw new DllNotFoundException(
                $"SoundTouch.dll was not found. Configure {nameof(SoundTouchRuntime)} with the directory that contains win-x64/win-x86 SoundTouch binaries. Current root: {SoundTouchRuntime.RuntimeRoot}",
                ex);
        }

        if (_handle.IsInvalid)
        {
            throw new InvalidOperationException("SoundTouch failed to create a processor instance.");
        }
    }

    public string VersionString
    {
        get
        {
            if (_versionString != null) return _versionString;

            var buffer = new byte[100];
            SoundTouchNativeMethods.GetVersionString(buffer, buffer.Length);
            var zeroIndex = Array.IndexOf(buffer, (byte)0);
            _versionString = Encoding.UTF8.GetString(buffer, 0, zeroIndex >= 0 ? zeroIndex : buffer.Length);
            return _versionString;
        }
    }

    public bool IsEmpty => SoundTouchNativeMethods.IsEmpty(_handle) != 0;

    public int NumberOfSamplesAvailable => (int)SoundTouchNativeMethods.NumSamples(_handle);

    public int NumberOfUnprocessedSamples => SoundTouchNativeMethods.NumUnprocessedSamples(_handle);

    public void SetPitchOctaves(float pitchOctaves)
    {
        SoundTouchNativeMethods.SetPitchOctaves(_handle, pitchOctaves);
    }

    public void SetSampleRate(int sampleRate)
    {
        SoundTouchNativeMethods.SetSampleRate(_handle, (uint)sampleRate);
    }

    public void SetChannels(int channels)
    {
        SoundTouchNativeMethods.SetChannels(_handle, (uint)channels);
    }

    public void PutSamples(float[] samples, int numSamples)
    {
        SoundTouchNativeMethods.PutSamples(_handle, samples, numSamples);
    }

    public int ReceiveSamples(float[] outBuffer, int maxSamples)
    {
        return (int)SoundTouchNativeMethods.ReceiveSamples(_handle, outBuffer, (uint)maxSamples);
    }

    public void Flush()
    {
        SoundTouchNativeMethods.Flush(_handle);
    }

    public void Clear()
    {
        SoundTouchNativeMethods.Clear(_handle);
    }

    public void SetRate(float newRate)
    {
        SoundTouchNativeMethods.SetRate(_handle, newRate);
    }

    public void SetTempo(float newTempo)
    {
        SoundTouchNativeMethods.SetTempo(_handle, newTempo);
    }

    public int GetUseAntiAliasing()
    {
        return SoundTouchNativeMethods.GetSetting(_handle, SoundTouchSettings.UseAaFilter);
    }

    public void SetUseAntiAliasing(bool useAntiAliasing)
    {
        SoundTouchNativeMethods.SetSetting(_handle, SoundTouchSettings.UseAaFilter, useAntiAliasing ? 1 : 0);
    }

    public int GetUseQuickSeek()
    {
        return SoundTouchNativeMethods.GetSetting(_handle, SoundTouchSettings.UseQuickSeek);
    }

    public void SetUseQuickSeek(bool useQuickSeek)
    {
        SoundTouchNativeMethods.SetSetting(_handle, SoundTouchSettings.UseQuickSeek, useQuickSeek ? 1 : 0);
    }

    public void Dispose()
    {
        _handle.Dispose();
    }
}
