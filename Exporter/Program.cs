using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Milky.OsuPlayer.Media.Audio.Player;
using Milky.OsuPlayer.Shared.Models.NostModels;
using NAudio.Lame;
using NAudio.Wave;
using Nostool.Audio;
using Nostool.Composer;
using Nostool.Xwb;

namespace Nostool
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var composer =
                new NostComposer(@"G:\GitHub\Osu-Player\Exporter\bin\Debug\net5.0-windows\test\op\music_list.xml");
            await composer.ComposeSingleByFolder(
                @"G:\GitHub\Osu-Player\Exporter\bin\Debug\net5.0-windows\test\op\music\");
            //var single = new SingleExtractor(@"C:\Users\milki\Downloads\xwb_split_112\key_cat1.xwb");
            //await single.ExtractAsync();

            //MultiExtractor o;
            //o = new MultiExtractor(@"E:\milkitic\others\OP2\PAN-001-2019100200\contents\data\sound", ".\\test", "op");
            //await o.ExtractAsync();

            //Console.WriteLine("======Done op!======");
            //o = new MultiExtractor(@"E:\milkitic\others\OP2\PAN-001-2019100200\contents\data_op2\sound", ".\\test", "op2");
            //await o.ExtractAsync();
            //Console.WriteLine("======Done op2!======");

            return;
            var sw = Stopwatch.StartNew();
            var path = @"C:\Users\milki\Desktop\NNSongs\m_l0011_sugarsong_01hard.xml";
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
            var noteChannel = new NoteChannel(path, 0.7f, 0.7f, mScore, engine);
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
            var filename = Path.GetFileNameWithoutExtension(path);
            using var outStream = new FileStream("E:\\" + filename + ".mp3", FileMode.Create, FileAccess.Write);
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
}
