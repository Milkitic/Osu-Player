//////////////////////////////////////////////////////////////////////////////
///
/// C# wrapper to access SoundTouch APIs from an external SoundTouch.dll library
///
/// Author        : Copyright (c) Olli Parviainen
/// Author e-mail : oparviai 'at' iki.fi
/// SoundTouch WWW: http://www.surina.net/soundtouch
///
/// The C# wrapper improved by Mario Di Vece
///
////////////////////////////////////////////////////////////////////////////////
//
// License :
//
//  SoundTouch audio processing library
//  Copyright (c) Olli Parviainen
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 2.1 of the License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;

namespace OsuPlayer.Media.Audio.SoundTouch;

internal sealed class SoundTouch : IDisposable
{
    private const int VersionBufferSize = 100;

    private readonly object _syncRoot = new();
    private readonly SoundTouchHandle _handle;
    private string? _versionString;
    private bool _disposed;

    public SoundTouch()
    {
        SoundTouchRuntime.EnsureSupported();

        try
        {
            _handle = SoundTouchNativeMethods.CreateInstance();
        }
        catch (DllNotFoundException ex)
        {
            throw new DllNotFoundException(
                $"SoundTouch.dll was not found. Configure {nameof(SoundTouchRuntime)} with the directory that contains SoundTouch binaries. Current root: {SoundTouchRuntime.RuntimeRoot}",
                ex);
        }

        if (_handle.IsInvalid)
        {
            throw new InvalidOperationException("SoundTouch failed to create a processor instance.");
        }
    }

    public enum Setting
    {
        UseAntiAliasFilter = 0,
        AntiAliasFilterLength = 1,
        UseQuickSeek = 2,
        SequenceMilliseconds = 3,
        SeekWindowMilliseconds = 4,
        OverlapMilliseconds = 5,
        NominalInputSequence = 6,
        NominalOutputSequence = 7,
        InitialLatency = 8,
    }

    public static string Version => GetNativeVersionString();

    public static bool IsAvailable
    {
        get
        {
            try
            {
                return SoundTouchNativeMethods.GetVersionId() != 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public string VersionString
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                _versionString ??= GetNativeVersionString();
                return _versionString;
            }
        }
    }

    public int VersionId => SoundTouchNativeMethods.GetVersionId();

    public uint AvailableSampleCount
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return SoundTouchNativeMethods.NumSamples(_handle);
            }
        }
    }

    public uint UnprocessedSampleCount
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return SoundTouchNativeMethods.NumUnprocessedSamples(_handle);
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return SoundTouchNativeMethods.IsEmpty(_handle) != 0;
            }
        }
    }

    public int NumberOfSamplesAvailable
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return checked((int)SoundTouchNativeMethods.NumSamples(_handle));
            }
        }
    }

    public int NumberOfUnprocessedSamples
    {
        get
        {
            lock (_syncRoot)
            {
                ThrowIfDisposed();
                return checked((int)SoundTouchNativeMethods.NumUnprocessedSamples(_handle));
            }
        }
    }

    public uint Channels
    {
        set => SetChannels(value);
    }

    public uint SampleRate
    {
        set => SetSampleRate(value);
    }

    public float Tempo
    {
        set => SetTempo(value);
    }

    public float TempoChange
    {
        set => SetTempoChange(value);
    }

    public float Rate
    {
        set => SetRate(value);
    }

    public float RateChange
    {
        set => SetRateChange(value);
    }

    public float Pitch
    {
        set => SetPitch(value);
    }

    public float PitchOctaves
    {
        set => SetPitchOctaves(value);
    }

    public float PitchSemiTones
    {
        set => SetPitchSemiTones(value);
    }

    public int this[Setting settingId]
    {
        get => GetSetting(settingId);
        set => SetSetting(settingId, value);
    }

    public void SetChannels(int channels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
        SetChannels((uint)channels);
    }

    public void SetChannels(uint channels)
    {
        if (channels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), channels, "Value must be greater than zero.");
        }

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetChannels(_handle, channels);
        }
    }

    public void SetSampleRate(int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        SetSampleRate((uint)sampleRate);
    }

    public void SetSampleRate(uint sampleRate)
    {
        if (sampleRate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Value must be greater than zero.");
        }

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetSampleRate(_handle, sampleRate);
        }
    }

    public void SetPitch(float pitch)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetPitch(_handle, NormalizePositive(pitch, nameof(pitch)));
        }
    }

    public void SetPitchOctaves(float pitchOctaves)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetPitchOctaves(_handle, pitchOctaves);
        }
    }

    public void SetPitchSemiTones(float semitones)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetPitchSemiTones(_handle, semitones);
        }
    }

    public void SetRate(float newRate)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetRate(_handle, NormalizePositive(newRate, nameof(newRate)));
        }
    }

    public void SetRateChange(float newRate)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetRateChange(_handle, newRate);
        }
    }

    public void SetTempo(float newTempo)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetTempo(_handle, NormalizePositive(newTempo, nameof(newTempo)));
        }
    }

    public void SetTempoChange(float newTempo)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.SetTempoChange(_handle, newTempo);
        }
    }

    public void PutSamples(float[] samples, int numSamples)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numSamples);
        PutSamples(samples, (uint)numSamples);
    }

    public void PutSamples(float[] samples, uint numSamples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.PutSamples(_handle, samples, numSamples);
        }
    }

    public void PutSamplesI16(short[] samples, int numSamples)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numSamples);
        PutSamplesI16(samples, (uint)numSamples);
    }

    public void PutSamplesI16(short[] samples, uint numSamples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.PutSamplesI16(_handle, samples, numSamples);
        }
    }

    public int ReceiveSamples(float[] outBuffer, int maxSamples)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxSamples);
        return checked((int)ReceiveSamples(outBuffer, (uint)maxSamples));
    }

    public uint ReceiveSamples(float[] outBuffer, uint maxSamples)
    {
        ArgumentNullException.ThrowIfNull(outBuffer);

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            return SoundTouchNativeMethods.ReceiveSamples(_handle, outBuffer, maxSamples);
        }
    }

    public int ReceiveSamplesI16(short[] outBuffer, int maxSamples)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxSamples);
        return checked((int)ReceiveSamplesI16(outBuffer, (uint)maxSamples));
    }

    public uint ReceiveSamplesI16(short[] outBuffer, uint maxSamples)
    {
        ArgumentNullException.ThrowIfNull(outBuffer);

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            return SoundTouchNativeMethods.ReceiveSamplesI16(_handle, outBuffer, maxSamples);
        }
    }

    public void Flush()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.Flush(_handle);
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            SoundTouchNativeMethods.Clear(_handle);
        }
    }

    public int GetUseAntiAliasing()
    {
        return GetSetting(Setting.UseAntiAliasFilter);
    }

    public void SetUseAntiAliasing(bool useAntiAliasing)
    {
        SetSetting(Setting.UseAntiAliasFilter, useAntiAliasing ? 1 : 0);
    }

    public int GetUseQuickSeek()
    {
        return GetSetting(Setting.UseQuickSeek);
    }

    public void SetUseQuickSeek(bool useQuickSeek)
    {
        SetSetting(Setting.UseQuickSeek, useQuickSeek ? 1 : 0);
    }

    public int GetSetting(Setting setting)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            return SoundTouchNativeMethods.GetSetting(_handle, setting);
        }
    }

    public void SetSetting(Setting setting, int value)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            ThrowIfNativeCallFailed(
                SoundTouchNativeMethods.SetSetting(_handle, setting, value),
                nameof(SoundTouchNativeMethods.SetSetting));
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _handle.Dispose();
        }
    }

    private static string GetNativeVersionString()
    {
        var buffer = new byte[VersionBufferSize];
        SoundTouchNativeMethods.GetVersionString(buffer, buffer.Length);
        var zeroIndex = Array.IndexOf(buffer, (byte)0);
        return Encoding.UTF8.GetString(buffer, 0, zeroIndex >= 0 ? zeroIndex : buffer.Length);
    }

    private static float NormalizePositive(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be a finite positive number.");
        }

        return value;
    }

    private static void ThrowIfNativeCallFailed(int result, string operation)
    {
        if (result == 0)
        {
            throw new InvalidOperationException($"{operation} failed in SoundTouch.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
