using System;
using System.IO;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer;
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
            //var path = "E:\\Games\\osu!\\Songs\\take yf\\" +
            //           //"1376486 Risshuu feat. Choko - Take\\" +
            //           "Risshuu feat. Choko - Take (yf_bmp) [test].osu";

            var path = "E:\\Games\\osu!\\Songs\\3198 Rhapsody - Emerald Sword\\" +
                       //"1376486 Risshuu feat. Choko - Take\\" +
                       "Rhapsody - Emerald Sword (Reikin) [net].osu";

            var folder = Path.GetDirectoryName(path);
            var osuFile = await OsuFile.ReadFromFileAsync(path);
            if (!osuFile.ReadSuccess)
            {
                throw osuFile.ReadException;
            }

            var engine = new AudioPlaybackEngine();
            var mp3Path = Path.Combine(folder, osuFile?.General.AudioFilename ?? ".");

            var directChannel = new DirectChannel(mp3Path, osuFile.General.AudioLeadIn, engine);
            var hitsoundChannel = new HitsoundChannel(osuFile, engine);
            var sampleChannel = new SampleChannel(osuFile, engine, new Subchannel[]
            {
                directChannel, hitsoundChannel
            });

            var exporter = new Mp3Exporter(new MultiElementsChannel[] { directChannel, hitsoundChannel, sampleChannel });
            string pre = null;
            await exporter.ExportAsync("test.mp3", 192, null, progress =>
            {
                var p = progress.ToString("P0");
                if (pre != p)
                {
                    Console.WriteLine(p);
                    pre = p;
                }
            });
        }
    }
}
