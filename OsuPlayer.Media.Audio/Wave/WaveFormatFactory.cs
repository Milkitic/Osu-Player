using Milky.OsuPlayer.Common;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Wave
{
    /// <summary>
    /// Audio file to wave stream
    /// </summary>
    internal static class WaveFormatFactory
    {
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

        public static int SampleRate { get; set; } = 44100;

        public static int Bits { get; set; } = 16;
        public static int Channels { get; set; } = 2;

        public static WaveFormat IeeeWaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

        public static WaveFormat PcmWaveFormat => new WaveFormat(SampleRate, Bits, Channels);

        public static async Task<MyAudioFileReader> GetResampledAudioFileReader(string path, MyAudioFileReader.WaveStreamType type)
        {
            var stream = await Resample(path, type).ConfigureAwait(false);
            return stream is MyAudioFileReader afr ? afr : new MyAudioFileReader(stream, type);
        }

        public static async Task<MyAudioFileReader> GetResampledAudioFileReader(string path)
        {
            var cache = Path.Combine(Domain.CachePath,
                $"{Guid.NewGuid().ToString().Replace("-", "")}.sound");
            var stream = await Resample(path, cache).ConfigureAwait(false);
            return stream is MyAudioFileReader afr ? afr : new MyAudioFileReader(cache);
        }

        private static async Task<Stream> Resample(string path, string targetPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var audioFileReader = new MyAudioFileReader(path);
                    if (CompareWaveFormat(audioFileReader.WaveFormat))
                    {
                        return audioFileReader;
                    }

                    using (audioFileReader)
                    using (var resampler = new MediaFoundationResampler(audioFileReader, PcmWaveFormat))
                    using (var stream = new FileStream(targetPath, FileMode.Create))
                    {
                        resampler.ResamplerQuality = ResamplerQuality.Highest;
                        WaveFileWriter.WriteWavFileToStream(stream, resampler);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        private static async Task<Stream> Resample(string path, MyAudioFileReader.WaveStreamType type)
        {
            if (!File.Exists(path))
            {
                path = Path.Combine(Domain.DefaultPath, "blank.wav");
            }

            return await Task.Run(() =>
            {
                MyAudioFileReader audioFileReader = null;
                try
                {
                    audioFileReader = new MyAudioFileReader(path);
                    if (CompareWaveFormat(audioFileReader.WaveFormat))
                    {
                        return (Stream)audioFileReader;
                    }

                    using (audioFileReader)
                    {
                        if (type == MyAudioFileReader.WaveStreamType.Wav)
                        {
                            using (var resampler = new MediaFoundationResampler(audioFileReader, PcmWaveFormat))
                            {
                                var stream = new MemoryStream();
                                resampler.ResamplerQuality = 60;
                                WaveFileWriter.WriteWavFileToStream(stream, resampler);
                                stream.Position = 0;
                                return stream;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                catch (Exception ex)
                {
                    audioFileReader?.Dispose();
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        private static bool CompareWaveFormat(WaveFormat waveFormat)
        {
            var pcmWaveFormat = PcmWaveFormat;
            if (pcmWaveFormat.Channels != waveFormat.Channels) return false;
            if (pcmWaveFormat.SampleRate != waveFormat.SampleRate) return false;
            return true;
        }
    }
}
