using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Milky.OsuPlayer.Media.Music.SoundTouch
{
    public class SoundTouchApi : IDisposable
    {
        #region SoundTouch .NET wrapper API

        /// <summary>
        /// Create a new instance of SoundTouch processor.
        /// </summary>
        public void CreateInstance()
        {
            if (m_handle != IntPtr.Zero)
            {
                throw new ApplicationException("SoundSharp Instance was already initialized but not destroyed. Use DestroyInstance().");
            }

            m_handle = soundtouch_createInstance();
            GetVersionString();
            GetVersionId();
        }

        /// <summary>
        /// Destroys a SoundTouch processor instance.
        /// </summary>
        private void DestroyInstance()
        {
            if (m_handle != IntPtr.Zero)
            {
                soundtouch_destroyInstance(m_handle);
                m_handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Get SoundTouch library version string
        /// </summary>
        /// <returns></returns>
        public string GetVersionString()
        {
            StringBuilder versionString = new StringBuilder(100);
            soundtouch_getVersionString2(versionString, 100);

            return versionString.ToString();
        }

        /// <summary>
        /// Get SoundTouch library version Id
        /// </summary>
        /// <returns></returns>
        public int GetVersionId()
        {
            SoundTouchVersionId = soundtouch_getVersionId();

            return SoundTouchVersionId;
        }

        /// <summary>
        /// Sets new rate control value. Normal rate = 1.0, smaller values
        /// represent slower rate, larger faster rates.
        /// </summary>
        /// <param name="newRate"></param>
        public void SetRate(float newRate)
        {
            VerifyInstanceInitialized();

            soundtouch_setRate(m_handle, newRate);
        }

        /// <summary>
        /// Sets new tempo control value. Normal tempo = 1.0, smaller values
        /// represent slower tempo, larger faster tempo.
        /// </summary>
        /// <param name="newTempo"></param>
        public void SetTempo(float newTempo)
        {
            VerifyInstanceInitialized();

            soundtouch_setTempo(m_handle, newTempo);
        }

        /// <summary>
        /// Sets new rate control value as a difference in percents compared
        /// to the original rate (-50 .. +100 %);
        /// </summary>
        /// <param name="newRate"></param>
        public void SetRateChange(float newRate)
        {
            VerifyInstanceInitialized();

            soundtouch_setRateChange(m_handle, newRate);
        }

        /// <summary>
        /// Sets new tempo control value as a difference in percents compared
        /// to the original tempo (-50 .. +100 %)
        /// </summary>
        /// <param name="newRate"></param>
        public void SetTempoChange(float newTempo)
        {
            VerifyInstanceInitialized();

            soundtouch_setTempoChange(m_handle, newTempo);
        }

        /// <summary>
        /// Sets new pitch control value. Original pitch = 1.0, smaller values
        /// represent lower pitches, larger values higher pitch.
        /// </summary>
        /// <param name="newPitch"></param>
        public void SetPitch(float newPitch)
        {
            VerifyInstanceInitialized();

            soundtouch_setPitch(m_handle, newPitch);
        }

        /// <summary>
        /// Sets pitch change in octaves compared to the original pitch (-1.00 .. +1.00)
        /// </summary>
        /// <param name="newPitch"></param>
        public void SetPitchOctaves(float newPitch)
        {
            VerifyInstanceInitialized();

            soundtouch_setPitchOctaves(m_handle, newPitch);
        }

        /// <summary>
        /// Sets pitch change in semi-tones compared to the original pitch (12 .. +12)
        /// </summary>
        /// <param name="newPitch"></param>
        public void SetPitchSemiTones(float newPitch)
        {
            VerifyInstanceInitialized();

            soundtouch_setPitchSemiTones(m_handle, newPitch);
        }


        /// <summary>
        /// Sets the number of channels, 1 = mono, 2 = stereo
        /// </summary>
        /// <param name="numChannels"></param>
        public void SetChannels(int numChannels)
        {
            VerifyInstanceInitialized();

            soundtouch_setChannels(m_handle, (uint)numChannels);
        }

        /// <summary>
        /// Sets sample rate.
        /// </summary>
        /// <param name="srate"></param>
        public void SetSampleRate(int srate)
        {
            VerifyInstanceInitialized();

            soundtouch_setSampleRate(m_handle, (uint)srate);
        }

        /// <summary>
        /// Flushes the last samples from the processing pipeline to the output.
        /// Clears also the internal processing buffers.
        //
        /// Note: This function is meant for extracting the last samples of a sound
        /// stream. This function may introduce additional blank samples in the end
        /// of the sound stream, and thus it's not recommended to call this function
        /// in the middle of a sound stream.
        /// </summary>
        public void Flush()
        {
            VerifyInstanceInitialized();

            soundtouch_flush(m_handle);
        }

        /// <summary>
        /// Adds 'numSamples' pcs of samples from the 'samples' memory position into
        /// the input of the object. Notice that sample rate _has_to_ be set before
        /// calling this function, otherwise throws a runtime_error exception.
        /// </summary>
        /// <param name="pSamples"></param>
        /// <param name="numSamples"></param>
        public void PutSamples(float[] pSamples, uint numSamples)
        {
            VerifyInstanceInitialized();

            soundtouch_putSamples(m_handle, pSamples, numSamples);
        }


        /// <summary>
        /// Clears all the samples in the object's output and internal processing buffers.
        /// </summary>
        public void Clear()
        {
            VerifyInstanceInitialized();

            soundtouch_clear(m_handle);
        }

        /// <summary>
        /// Changes a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        /// 
        /// \return 'TRUE' if the setting was succesfully changed
        /// </summary>
        public void SetSetting(SoundTouchSettings settingId, int value)
        {
            VerifyInstanceInitialized();

            soundtouch_setSetting(m_handle, (int)settingId, value);
        }

        /// <summary>
        /// Reads a setting controlling the processing system behaviour. 
        /// See the 'SETTING_...' defines for available setting ID's.
        /// </summary>
        /// <param name="settingId"></param>
        /// <returns>Returns the setting value.</returns>
        public int GetSetting(SoundTouchSettings settingId)
        {
            VerifyInstanceInitialized();

            return soundtouch_getSetting(m_handle, (int)settingId);
        }

        /// <summary>
        /// Returns number of samples currently unprocessed.
        /// </summary>
        /// <returns></returns>
        public int GetNumUnprocessedSamples()
        {
            VerifyInstanceInitialized();

            return soundtouch_numUnprocessedSamples(m_handle);
        }

        /// <summary>
        /// Adjusts book-keeping so that given number of samples are removed from beginning of the 
        /// sample buffer without copying them anywhere. 
        ///
        /// Used to reduce the number of samples in the buffer when accessing the sample buffer directly
        /// with 'ptrBegin' function.
        /// </summary>
        /// <param name="pOutBuffer"></param>
        /// <param name="maxSamples"></param>
        /// <returns></returns>
        public uint ReceiveSamples(float[] pOutBuffer, uint maxSamples)
        {
            VerifyInstanceInitialized();

            return soundtouch_receiveSamples(m_handle, pOutBuffer, maxSamples);
        }

        /// <summary>
        /// Returns number of samples currently available.
        /// </summary>
        /// <returns></returns>
        public int GetNumSamples()
        {
            VerifyInstanceInitialized();

            return soundtouch_numSamples(m_handle);
        }


        /// <summary>
        /// Returns nonzero if there aren't any samples available for outputting.
        /// </summary>
        /// <returns></returns>
        public int IsEmpty()
        {
            VerifyInstanceInitialized();

            return soundtouch_isEmpty(m_handle);
        }

        public enum SoundTouchSettings
        {
            /// <summary>
            /// Available setting IDs for the 'setSetting' and 'get_setting' functions.
            /// Enable/disable anti-alias filter in pitch transposer (0 = disable)
            /// </summary>
            SETTING_USE_AA_FILTER = 0,

            /// <summary>
            /// Pitch transposer anti-alias filter length (8 .. 128 taps, default = 32)
            /// </summary>
            SETTING_AA_FILTER_LENGTH = 1,

            /// <summary>
            /// Enable/disable quick seeking algorithm in tempo changer routine
            /// (enabling quick seeking lowers CPU utilization but causes a minor sound
            ///  quality compromising)
            /// </summary>
            SETTING_USE_QUICKSEEK = 2,

            /// <summary>
            /// Time-stretch algorithm single processing sequence length in milliseconds. This determines 
            /// to how long sequences the original sound is chopped in the time-stretch algorithm. 
            /// See "STTypes.h" or README for more information.
            /// </summary>
            SETTING_SEQUENCE_MS = 3,

            /// <summary>
            /// Time-stretch algorithm seeking window length in milliseconds for algorithm that finds the 
            /// best possible overlapping location. This determines from how wide window the algorithm 
            /// may look for an optimal joining location when mixing the sound sequences back together. 
            /// See "STTypes.h" or README for more information.
            /// </summary>
            SETTING_SEEKWINDOW_MS = 4,

            /// <summary>
            /// Time-stretch algorithm overlap length in milliseconds. When the chopped sound sequences 
            /// are mixed back together, to form a continuous sound stream, this parameter defines over 
            /// how long period the two consecutive sequences are let to overlap each other. 
            /// See "STTypes.h" or README for more information.
            /// </summary>
            SETTING_OVERLAP_MS = 5
        };


        #endregion

        #region SoundSharp Native API - DLL Imports

        public const string SoundTouchDLLName = "./plugins/SoundTouch.dll";

        #region C DLL Header
        /*

/// Create a new instance of SoundTouch processor.
SOUNDTOUCHDLL_API HANDLE __stdcall soundtouch_createInstance();

/// Destroys a SoundTouch processor instance.
SOUNDTOUCHDLL_API void __stdcall soundtouch_destroyInstance(HANDLE h);

/// Get SoundTouch library version string
SOUNDTOUCHDLL_API const char *__stdcall soundtouch_getVersionString();

/// Get SoundTouch library version Id
SOUNDTOUCHDLL_API unsigned int __stdcall soundtouch_getVersionId();

/// Sets new rate control value. Normal rate = 1.0, smaller values
/// represent slower rate, larger faster rates.
SOUNDTOUCHDLL_API void __stdcall soundtouch_setRate(HANDLE h, float newRate);

/// Sets new tempo control value. Normal tempo = 1.0, smaller values
/// represent slower tempo, larger faster tempo.
SOUNDTOUCHDLL_API void __stdcall soundtouch_setTempo(HANDLE h, float newTempo);

/// Sets new rate control value as a difference in percents compared
/// to the original rate (-50 .. +100 %);
SOUNDTOUCHDLL_API void __stdcall soundtouch_setRateChange(HANDLE h, float newRate);

/// Sets new tempo control value as a difference in percents compared
/// to the original tempo (-50 .. +100 %);
SOUNDTOUCHDLL_API void __stdcall soundtouch_setTempoChange(HANDLE h, float newTempo);

/// Sets new pitch control value. Original pitch = 1.0, smaller values
/// represent lower pitches, larger values higher pitch.
SOUNDTOUCHDLL_API void __stdcall soundtouch_setPitch(HANDLE h, float newPitch);

/// Sets pitch change in octaves compared to the original pitch  
/// (-1.00 .. +1.00);
SOUNDTOUCHDLL_API void __stdcall soundtouch_setPitchOctaves(HANDLE h, float newPitch);

/// Sets pitch change in semi-tones compared to the original pitch
/// (-12 .. +12);
SOUNDTOUCHDLL_API void __stdcall soundtouch_setPitchSemiTones(HANDLE h, float newPitch);


/// Sets the number of channels, 1 = mono, 2 = stereo
SOUNDTOUCHDLL_API void __stdcall soundtouch_setChannels(HANDLE h, unsigned int numChannels);

/// Sets sample rate.
SOUNDTOUCHDLL_API void __stdcall soundtouch_setSampleRate(HANDLE h, unsigned int srate);

/// Flushes the last samples from the processing pipeline to the output.
/// Clears also the internal processing buffers.
//
/// Note: This function is meant for extracting the last samples of a sound
/// stream. This function may introduce additional blank samples in the end
/// of the sound stream, and thus it's not recommended to call this function
/// in the middle of a sound stream.
SOUNDTOUCHDLL_API void __stdcall soundtouch_flush(HANDLE h);

/// Adds 'numSamples' pcs of samples from the 'samples' memory position into
/// the input of the object. Notice that sample rate _has_to_ be set before
/// calling this function, otherwise throws a runtime_error exception.
SOUNDTOUCHDLL_API void __stdcall soundtouch_putSamples(HANDLE h, 
        const float *samples,       ///< Pointer to sample buffer.
        unsigned int numSamples     ///< Number of samples in buffer. Notice
                                    ///< that in case of stereo-sound a single sample
                                    ///< contains data for both channels.
        );

/// Clears all the samples in the object's output and internal processing
/// buffers.
SOUNDTOUCHDLL_API void __stdcall soundtouch_clear(HANDLE h);

/// Changes a setting controlling the processing system behaviour. See the
/// 'SETTING_...' defines for available setting ID's.
/// 
/// \return 'TRUE' if the setting was succesfully changed
SOUNDTOUCHDLL_API BOOL __stdcall soundtouch_setSetting(HANDLE h, 
                int settingId,   ///< Setting ID number. see SETTING_... defines.
                int value        ///< New setting value.
                );

/// Reads a setting controlling the processing system behaviour. See the
/// 'SETTING_...' defines for available setting ID's.
///
/// \return the setting value.
SOUNDTOUCHDLL_API int __stdcall soundtouch_getSetting(HANDLE h, 
                          int settingId    ///< Setting ID number, see SETTING_... defines.
                );


/// Returns number of samples currently unprocessed.
SOUNDTOUCHDLL_API unsigned int __stdcall soundtouch_numUnprocessedSamples(HANDLE h);

/// Adjusts book-keeping so that given number of samples are removed from beginning of the 
/// sample buffer without copying them anywhere. 
///
/// Used to reduce the number of samples in the buffer when accessing the sample buffer directly
/// with 'ptrBegin' function.
SOUNDTOUCHDLL_API unsigned int __stdcall soundtouch_receiveSamples(HANDLE h, 
            float *outBuffer,           ///< Buffer where to copy output samples.
            unsigned int maxSamples     ///< How many samples to receive at max.
            );

/// Returns number of samples currently available.
SOUNDTOUCHDLL_API unsigned int __stdcall soundtouch_numSamples(HANDLE h);

/// Returns nonzero if there aren't any samples available for outputting.
SOUNDTOUCHDLL_API int __stdcall soundtouch_isEmpty(HANDLE h);

*/
        #endregion

        [DllImport(SoundTouchDLLName)]
        internal static extern IntPtr soundtouch_createInstance();

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_destroyInstance(IntPtr h);

        [DllImport(SoundTouchDLLName)]
        internal static extern int soundtouch_getVersionId();

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_getVersionString2(StringBuilder versionString, int bufferSize);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setRate(IntPtr h, float newRate);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setTempo(IntPtr h, float newTempo);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setRateChange(IntPtr h, float newRate);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setTempoChange(IntPtr h, float newTempo);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setPitch(IntPtr h, float newPitch);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setPitchOctaves(IntPtr h, float newPitch);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setPitchSemiTones(IntPtr h, float newPitch);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setChannels(IntPtr h, uint numChannels);

        /// Sets sample rate.
        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_setSampleRate(IntPtr h, uint srate);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_flush(IntPtr h);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_putSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] samples, uint numSamples);

        [DllImport(SoundTouchDLLName)]
        internal static extern void soundtouch_clear(IntPtr h);

        [DllImport(SoundTouchDLLName)]
        internal static extern bool soundtouch_setSetting(IntPtr h, int settingId, int value);

        [DllImport(SoundTouchDLLName)]
        internal static extern int soundtouch_getSetting(IntPtr h, int settingId);

        [DllImport(SoundTouchDLLName)]
        internal static extern int soundtouch_numUnprocessedSamples(IntPtr h);

        [DllImport(SoundTouchDLLName)]
        internal static extern uint soundtouch_receiveSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] outBuffer, uint maxSamples);

        [DllImport(SoundTouchDLLName)]
        internal static extern int soundtouch_numSamples(IntPtr h);

        [DllImport(SoundTouchDLLName)]
        internal static extern int soundtouch_isEmpty(IntPtr h);

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            DestroyInstance();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Helper function for validating the SoundTouch as initialized
        /// </summary>
        private void VerifyInstanceInitialized()
        {
            if (m_handle == IntPtr.Zero)
            {
                throw new ApplicationException("SoundTouch as not initialized. Use CreateInstance()");
            }
        }

        #endregion

        #region Members

        private IntPtr m_handle = IntPtr.Zero;

        public string SoundTouchVersionString { get; private set; }
        public int SoundTouchVersionId { get; private set; }

        #endregion
    }

    #region ByteAndFloatsConverter - Conversion Utility Structure (Byte[] <-> Float[])

    /// <summary>
    /// Helper Structure - Allows "C-Style forced pointer" conversion of Bytes array to Floats array and visa versa (C# does not allow this)
    /// The main benefit is performance - no need to iterate and convert each element in the array
    /// Taken from: http://stackoverflow.com/questions/619041/what-is-the-fastest-way-to-convert-a-float-to-a-byte
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ByteAndFloatsConverter
    {
        [FieldOffset(0)]
        public Byte[] Bytes;

        [FieldOffset(0)]
        public float[] Floats;
    }

    #endregion
}
