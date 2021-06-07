using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Media.Audio.Player.Subchannels;
using Milky.OsuPlayer.Shared.Models.NostModels;
using NAudio.Lame;
using NAudio.Utils;
using NAudio.Wave;
using Newtonsoft.Json;

namespace Exporter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            var path = @"C:\Users\milki\Desktop\NNSongs\m_l0002_connect_01hard.xml";
            //var o = File.ReadAllText(path);
            XmlSerializer serializer = new XmlSerializer(typeof(MusicScore));
            StreamReader xmlreader = new StreamReader(path);
            var mScore = serializer.Deserialize(xmlreader) as MusicScore;

            await Write(path, mScore);
            await Write(path, mScore);
            Console.WriteLine("total: " + sw.Elapsed);
            //WaveFileWriter.WriteWavFileToStream(outStream, sourceProvider);
        }

        private static async Task Write(string path, MusicScore mScore)
        {
            var sw2 = Stopwatch.StartNew();
            var engine = new AudioPlaybackEngine();
            var noteChannel = new NoteChannel(path, mScore, engine);
            await noteChannel.Initialize();

            Console.WriteLine("init: " + sw2.Elapsed);
            sw2.Restart();
            string p = null;
            engine.Updated += (e, t1, t2) =>
            {
                noteChannel.TakeElements((int)t2.TotalMilliseconds).Wait();

                var progress = $"Progress: {t2.TotalMilliseconds / noteChannel.ChannelEndTime.TotalMilliseconds:P0}";
                if (p != progress)
                {
                    Console.WriteLine(progress);
                    p = progress;
                }

                if (t2 > noteChannel.ChannelEndTime)
                {
                    engine.RootMixer.ReadFully = false;
                    noteChannel.Submixer.ReadFully = false;
                }
            };

            var sourceProvider = engine.Root.ToWaveProvider();
            sourceProvider = new WaveFloatTo16Provider(sourceProvider);

            //using var outStream = new MemoryStream();
            using var outStream = new FileStream("F:\\test.mp3", FileMode.Create, FileAccess.Write);
            var writer = new LameMP3FileWriter(outStream, sourceProvider.WaveFormat, 320);

            //var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
            var buffer = new byte[128];
            while (true)
            {
                int count = sourceProvider.Read(buffer, 0, buffer.Length);
                if (count != 0)
                    writer.Write(buffer, 0, count);
                else
                    break;
            }

            outStream.Flush();
            Console.WriteLine("write: " + sw2.Elapsed);
        }

        private static void Engine_Updated(AudioPlaybackEngine engine, TimeSpan arg1, TimeSpan arg2)
        {
            //Console.Out.WriteLineAsync(arg2.ToString());

        }
    }

    public class NoteChannel : MultiElementsChannel
    {
        private readonly string _path;
        private readonly MusicScore _musicScore;

        public NoteChannel(string path, MusicScore musicScore, AudioPlaybackEngine engine)
            : base(engine)
        {
            _path = path;
            _musicScore = musicScore;
        }

        public override async Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            var dir = Path.GetDirectoryName(_path);
            var all = _musicScore.NoteData
                .SelectMany(k => k.SubNoteData);
            var ele = all
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1)
                .Select(k =>
                {
                    var s = _musicScore.TrackInfo.First(o => o.Index == k.TrackIndex).Name;
                    var isGeneric = Generics.Contains(s);

                    var name = s + "_" +
                               KeysoundFilenameUtilities.GetFileSuffix(k.ScalePiano);
                    var path = isGeneric
                        ? Path.Combine(Domain.DefaultPath, "generic", s, name)
                        : Path.Combine(dir, name);
                    return SoundElement.Create(k.StartTimingMsec, k.Velocity / 128f, 0, path + ".wav");
                });
            return new List<SoundElement>(ele);
        }
    }
}
