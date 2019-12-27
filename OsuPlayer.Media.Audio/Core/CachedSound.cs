using System.Collections.Generic;
using System.IO;
using System.Linq;
using Milky.OsuPlayer.Common;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio.Core
{
    public class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        private static readonly string CachePath = Path.Combine(Domain.CachePath, "_temp.sound");
        private static readonly object CacheLock = new object();

        public long Duration { get; private set; }
        public long Length { get; private set; }

        public CachedSound(string audioFileName)
        {
            lock (CacheLock)
            {
                WaveResampler.Resample(audioFileName, CachePath);

                using (var audioFileReader = new AudioFileReader(CachePath))
                {
                    WaveFormat = audioFileReader.WaveFormat;
                    var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                    var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                    int samplesRead;
                    while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                    {
                        wholeFile.AddRange(readBuffer.Take(samplesRead));
                    }

                    AudioData = wholeFile.ToArray();
                    Duration = audioFileReader.TotalTime.Milliseconds;
                    Length = audioFileReader.Length;
                }
            }
        }
    }
}