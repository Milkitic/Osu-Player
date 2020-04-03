using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Milky.OsuPlayer.Media.Audio.SoundTouch
{
    class SoundTouch : IDisposable
    {
        private IntPtr _handle;
        private string _versionString;
        private readonly bool _is64Bit;
        public SoundTouch()
        {
            _is64Bit = Marshal.SizeOf<IntPtr>() == 8;

            _handle = _is64Bit ? SoundTouchInterop64.soundtouch_createInstance() :
                SoundTouchInterop32.soundtouch_createInstance();
        }

        public string VersionString
        {
            get
            {
                if (_versionString == null)
                {
                    var s = new StringBuilder(100);
                    if (_is64Bit)
                        SoundTouchInterop64.soundtouch_getVersionString2(s, s.Capacity);
                    else
                        SoundTouchInterop32.soundtouch_getVersionString2(s, s.Capacity);
                    _versionString = s.ToString();
                }
                return _versionString;
            }
        }

        public void SetPitchOctaves(float pitchOctaves)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setPitchOctaves(_handle, pitchOctaves);
            else
                SoundTouchInterop32.soundtouch_setPitchOctaves(_handle, pitchOctaves);
        }

        public void SetSampleRate(int sampleRate)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setSampleRate(_handle, (uint)sampleRate);
            else
                SoundTouchInterop32.soundtouch_setSampleRate(_handle, (uint)sampleRate);
        }

        public void SetChannels(int channels)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setChannels(_handle, (uint)channels);
            else
                SoundTouchInterop32.soundtouch_setChannels(_handle, (uint)channels);
        }

        private void DestroyInstance()
        {
            if (_handle != IntPtr.Zero)
            {
                if (_is64Bit)
                    SoundTouchInterop64.soundtouch_destroyInstance(_handle);
                else
                    SoundTouchInterop32.soundtouch_destroyInstance(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            DestroyInstance();
            GC.SuppressFinalize(this);
        }

        ~SoundTouch()
        {
            DestroyInstance();
        }

        public void PutSamples(float[] samples, int numSamples)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_putSamples(_handle, samples, numSamples);
            else
                SoundTouchInterop32.soundtouch_putSamples(_handle, samples, numSamples);
        }

        public int ReceiveSamples(float[] outBuffer, int maxSamples)
        {
            if (_is64Bit)
                return (int)SoundTouchInterop64.soundtouch_receiveSamples(_handle, outBuffer, (uint)maxSamples);
            return (int)SoundTouchInterop32.soundtouch_receiveSamples(_handle, outBuffer, (uint)maxSamples);
        }

        public bool IsEmpty
        {
            get
            {
                if (_is64Bit)
                    return SoundTouchInterop64.soundtouch_isEmpty(_handle) != 0;
                return SoundTouchInterop32.soundtouch_isEmpty(_handle) != 0;
            }
        }

        public int NumberOfSamplesAvailable
        {
            get
            {
                if (_is64Bit)
                    return (int)SoundTouchInterop64.soundtouch_numSamples(_handle);
                return (int)SoundTouchInterop32.soundtouch_numSamples(_handle);
            }
        }

        public int NumberOfUnprocessedSamples
        {
            get
            {
                if (_is64Bit)
                    return SoundTouchInterop64.soundtouch_numUnprocessedSamples(_handle);
                return SoundTouchInterop32.soundtouch_numUnprocessedSamples(_handle);
            }
        }

        public void Flush()
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_flush(_handle);
            else
                SoundTouchInterop32.soundtouch_flush(_handle);
        }

        public void Clear()
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_clear(_handle);
            else
                SoundTouchInterop32.soundtouch_clear(_handle);
        }

        public void SetRate(float newRate)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setRate(_handle, newRate);
            else
                SoundTouchInterop32.soundtouch_setRate(_handle, newRate);
        }

        public void SetTempo(float newTempo)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setTempo(_handle, newTempo);
            else
                SoundTouchInterop32.soundtouch_setTempo(_handle, newTempo);
        }

        public int GetUseAntiAliasing()
        {
            if (_is64Bit)
                return SoundTouchInterop64.soundtouch_getSetting(_handle, SoundTouchSettings.UseAaFilter);
            return SoundTouchInterop32.soundtouch_getSetting(_handle, SoundTouchSettings.UseAaFilter);
        }

        public void SetUseAntiAliasing(bool useAntiAliasing)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setSetting(_handle, SoundTouchSettings.UseAaFilter, useAntiAliasing ? 1 : 0);
            else
                SoundTouchInterop32.soundtouch_setSetting(_handle, SoundTouchSettings.UseAaFilter, useAntiAliasing ? 1 : 0);
        }

        public void SetUseQuickSeek(bool useQuickSeek)
        {
            if (_is64Bit)
                SoundTouchInterop64.soundtouch_setSetting(_handle, SoundTouchSettings.UseQuickSeek, useQuickSeek ? 1 : 0);
            else
                SoundTouchInterop32.soundtouch_setSetting(_handle, SoundTouchSettings.UseQuickSeek, useQuickSeek ? 1 : 0);
        }

        public int GetUseQuickSeek()
        {
            if (_is64Bit)
                return SoundTouchInterop64.soundtouch_getSetting(_handle, SoundTouchSettings.UseQuickSeek);
            return SoundTouchInterop32.soundtouch_getSetting(_handle, SoundTouchSettings.UseQuickSeek);
        }
    }
}
