using System;
using System.IO;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.Exporters;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;
using Milky.OsuPlayer.Media.Audio;
using NAudio.Wave;

namespace ExportTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Configuration.Instance.SetLogger(LoggerFactory.Create(k => k.AddConsole()));

            await ExportOsu();
        }

        private static async Task ExportOsu()
        {
            var path = "F:\\milkitic\\Songs\\" +
                       "1346316 Nekomata Master feat. Mimi Nyami - TWINKLING\\" +
                       "Nekomata Master feat. Mimi Nyami - TWINKLING (yf_bmp) [Another].osu";

            //var path = "E:\\Games\\osu!\\Songs\\3198 Rhapsody - Emerald Sword\\" +
            //           //"1376486 Risshuu feat. Choko - Take\\" +
            //           "Rhapsody - Emerald Sword (Reikin) [net].osu";

            var folder = Path.GetDirectoryName(path);
            var osuFile = await OsuFile.ReadFromFileAsync(path);
            using var engine = new AudioPlaybackEngine(default(IWavePlayer));
            var mp3Path = Path.Combine(folder, osuFile?.General.AudioFilename ?? ".");

            var fileCache = new FileCache();

            await using var directChannel = new DirectChannel(mp3Path, osuFile.General.AudioLeadIn, engine);
            await using var hitsoundChannel = new HitsoundChannel(osuFile, engine, fileCache)
            {
                ManualOffset = 50,
            };
            await using var sampleChannel = new SampleChannel(osuFile, engine, new Subchannel[]
            {
                directChannel, hitsoundChannel
            }, fileCache)
            {
                ManualOffset = 50,
            };

            var exporter = new Mp3Exporter(new MultiElementsChannel[] { directChannel, hitsoundChannel, sampleChannel }, engine);
            string pre = null;
            await exporter.ExportAsync("test.mp3", progress =>
            {
                var p = $"Progress: {progress:P0}";
                if (pre != p)
                {
                    Console.WriteLine(p);
                    pre = p;
                }
            });
        }
    }
}
