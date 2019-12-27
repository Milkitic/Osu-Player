using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Audio.Core.SampleProviders;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio
{
    public static class WaveResampler
    {
        public static void Resample(string originPath, string targetPath)
        {
            try
            {
                int outRate = 44100;
                int channels = 2;
                var outFormat = new WaveFormat(outRate, channels);
                using (var audioFileReader = new MyAudioFileReader(originPath))
                using (var resampler = new MediaFoundationResampler(audioFileReader, outFormat))
                using (var stream = new FileStream(targetPath, FileMode.Create))
                {
                    resampler.ResamplerQuality = 60;
                    WaveFileWriter.WriteWavFileToStream(stream, resampler);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(originPath);
                throw;
            }
        }
    }
}
