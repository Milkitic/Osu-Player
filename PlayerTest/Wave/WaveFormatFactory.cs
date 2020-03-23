using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace PlayerTest.Wave
{
    /// <summary>
    /// Audio file to wave stream
    /// </summary>
    internal static class WaveFormatFactory
    {
        public static int SampleRate { get; set; } = 44100;
        public static int Channels { get; set; } = 2;
        public static WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

        public static async Task Resample(string path, string targetPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var audioFileReader = new MyAudioFileReader(path))
                    using (var resampler = new MediaFoundationResampler(audioFileReader, WaveFormat))
                    using (var stream = new FileStream(targetPath, FileMode.Create))
                    {
                        resampler.ResamplerQuality = ResamplerQuality.Highest;
                        WaveFileWriter.WriteWavFileToStream(stream, resampler);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        public static async Task<MemoryStream> Resample(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var audioFileReader = new AudioFileReader(path))
                    using (var resampler = new MediaFoundationResampler(audioFileReader, WaveFormat))
                    {
                        var stream = new MemoryStream();
                        resampler.ResamplerQuality = 60;
                        WaveFileWriter.WriteWavFileToStream(stream, resampler);
                        return stream;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        public struct ResamplerQuality
        {
            public int Quality { get; }

            public ResamplerQuality(int quality)
            {
                Quality = quality;
            }

            public static implicit operator int(ResamplerQuality quality)
            {
                return quality.Quality;
            }

            public static implicit operator ResamplerQuality(int quality)
            {
                return new ResamplerQuality(quality);
            }

            public static int Highest => 60;
            public static int Lowest => 1;
        }
    }
}
