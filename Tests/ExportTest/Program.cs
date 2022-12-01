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
            var path = @"C:\Users\milkitic\Downloads\ETERNAL DRAIN\ (bms2osu) [lv.11].osu";

            //var path = "E:\\Games\\osu!\\Songs\\3198 Rhapsody - Emerald Sword\\" +
            //           //"1376486 Risshuu feat. Choko - Take\\" +
            //           "Rhapsody - Emerald Sword (Reikin) [net].osu";

            var folder = Path.GetDirectoryName(path);
            var osuFile = await OsuFile.ReadFromFileAsync(path);
            if (!osuFile.ReadSuccess)
            {
                throw osuFile.ReadException;
            }

            using var engine = new AudioPlaybackEngine();
            var mp3Path = Path.Combine(folder, osuFile?.General.AudioFilename ?? ".");

            var fileCache = new FileCache();

            await using var directChannel = new DirectChannel(mp3Path, osuFile.General.AudioLeadIn, engine)
            {
                Volume = 1
            };
            await using var hitsoundChannel = new HitsoundChannel(osuFile, engine, fileCache)
            {
                ManualOffset = 0,
                Volume = 0.4f,
                BalanceFactor = 0f
            };
            await using var sampleChannel = new SampleChannel(osuFile, engine, new Subchannel[]
            {
                directChannel, hitsoundChannel
            }, fileCache)
            {
                ManualOffset = 0,
                Volume = 0.4f,
                BalanceFactor = 0f
            };

            var exporter = new WavPcmExporter(new MultiElementsChannel[] { directChannel, hitsoundChannel, sampleChannel }
                , engine);
            string pre = null;
            await exporter.ExportAsync("ETERNAL DRAIN.wav", progress =>
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
