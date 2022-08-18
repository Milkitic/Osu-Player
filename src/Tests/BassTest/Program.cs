using System;
using System.IO;
using ManagedBass;
using NAudio.FileFormats.Mp3;
using NAudio.Wave;
using WaveFileWriter = NAudio.Wave.WaveFileWriter;
using WaveFormat = NAudio.Wave.WaveFormat;

namespace BassTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = @"C:\Users\milkitic\Downloads\HuΣeR Vs. SYUNN feat.いちか - 狂水一華.mp3";
            var obj = new AudioDataHelper(fileName);
            var data = obj.GetData(out var waveFormat);

            var memoryStream = new MemoryStream(data);
            var waveStream = new RawSourceWaveStream(memoryStream, waveFormat);
            var p = new WaveFloatTo16Provider(waveStream);
            WaveFileWriter.CreateWaveFile("a.wav", p);

            var reader = new Mp3FileReaderBase(fileName, format => new DmoMp3FrameDecompressor(format));
            WaveFileWriter.CreateWaveFile("b.wav", reader);
        }
    }

    public class AudioDataHelper : IDisposable
    {
        private readonly string _fileName;
        private int _channel;

        public AudioDataHelper(string fileName)
        {
            _fileName = fileName;
            Bass.FloatingPointDSP = true;
            if (!Bass.Init())
                throw new Exception("Can't initialize device");
        }

        public byte[] GetData(out WaveFormat waveFormat)
        {
            Bass.MusicFree(_channel);

            string fileName = _fileName;

            if ((_channel = Bass.CreateStream(fileName, 0, 0, BassFlags.Float | BassFlags.Decode)) == 0)
            {
                if ((_channel = Bass.MusicLoad(fileName, 0, 0,
                   BassFlags.MusicSensitiveRamping | BassFlags.Float | BassFlags.Decode, 1)) == 0)
                {
                    throw new Exception("Can't open the file");
                }
            }

            Bass.ChannelGetInfo(_channel, out var info);
            waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(info.Frequency, info.Channels);
            if (info.Channels != 2)
            {
                Bass.MusicFree(_channel);
                Bass.StreamFree(_channel);

                throw new Exception("only stereo sources are supported");
            }

            var len = (int)Bass.ChannelGetLength(_channel);
            var bytes = new byte[len];
            var o = Bass.ChannelGetData(_channel, bytes, len);
            if (o == -1)
            {
                throw new Exception(Bass.LastError.ToString());
            }

            return bytes;
        }

        ~AudioDataHelper()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            Bass.Free();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}
