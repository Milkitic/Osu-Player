using System;
using System.Diagnostics;
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
            Configuration.Instance.SetLogger(LoggerFactory.Create(k => k.AddConsole()
                .AddFilter((str, level) =>
                {
                    return true;
                }))
            );

            await ExportOsu();
        }

        private static async Task ExportOsu()
        {
            var path = @"E:\Games\osu!\Songs\BmsToOsu\Black Lotus\wa. _  - Black Lotus (bms2osu) [lv.12].osu";
            //var path =
            //    @"C:\Users\milki\Downloads\Compressed\HappyFakeShow_Ponchi_feat_haxchi\obj_mathath -  (bms2osu) [lv.12 Sp Another].osu";
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
            //var mp3Path = @"E:\Games\osu!\Songs\146175 Hirata Hironobu - AI CATCH\audio.wav";

            var fileCache = new FileCache();

            await using var directChannel = new DirectChannel(mp3Path, 0, engine, new SampleControl()
            {
                Volume = 0.6f,
            })
            {
                BalanceFactor = 0,
                ManualOffset = 0
            };
            await using var hitsoundChannel = new HitsoundChannel(osuFile, engine, fileCache)
            {
                BalanceFactor = 0,
                Volume = 0.6f,
                ForceOffset = 20,
            };
            await using var sampleChannel = new SampleChannel(osuFile, engine, new Subchannel[]
            {
                directChannel, hitsoundChannel
            }, fileCache)
            {
                BalanceFactor = 0,
                Volume = 0.6f,
                //ManualOffset = 20,
            };

            var exporter = new Mp3Exporter(new MultiElementsChannel[]
            {
                hitsoundChannel,
                directChannel,
                sampleChannel
            }, engine);
            string pre = null;
            var preferredString = osuFile.Metadata.ArtistMeta.ToPreferredString() +
                                  " - " +
                                  osuFile.Metadata.TitleMeta.ToPreferredString() + ".mp3";
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                preferredString = preferredString.Replace(c, '_');
            }
            foreach (var c in Path.GetInvalidPathChars())
            {
                preferredString = preferredString.Replace(c, '_');
            }

            var sw = Stopwatch.StartNew();
            await exporter.ExportAsync(
                preferredString,
                bitRate: 320000,
                progressCallback:
                progress =>
                {
                    var p = $"Progress: {progress:P0}";
                    if (pre != p)
                    {
                        Console.WriteLine(p);
                        pre = p;
                    }
                });
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}
