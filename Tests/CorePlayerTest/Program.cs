using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Microsoft.Extensions.Logging;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.Devices;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Audio.Mixing;

namespace CorePlayerTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Configuration.Instance.SetLogger(LoggerFactory.Create(k => k.AddConsole()));

            //var path = "E:\\Games\\osu!\\Songs\\3198 Rhapsody - Emerald Sword\\" +
            //           //"1376486 Risshuu feat. Choko - Take\\" +
            //           "Rhapsody - Emerald Sword (Reikin) [net].osu";
            //var path = "E:\\Games\\osu!\\Songs\\take yf\\" +
            //           //"1376486 Risshuu feat. Choko - Take\\" +
            //           "Risshuu feat. Choko - Take (yf_bmp) [test].osu";
            var path = "F:\\milkitic\\Songs\\" +
                       "1376486 Risshuu feat. Choko - Take\\" +
                       "Risshuu feat. Choko - Take (yf_bmp) [Ta~ke take take take take take tatata~].osu";
            var folder = Path.GetDirectoryName(path);
            var osuFile = await OsuFile.ReadFromFileAsync(path);
            var engine = new AudioPlaybackEngine(new DeviceDescription()
            {
                WavePlayerType = WavePlayerType.WASAPI,
                FriendlyName = null,
                IsExclusive = false,
                Latency = 1
            });
            var player = new EsuPlayer(osuFile, engine);
            await player.InitializeAsync();

            bool finished = false;
            //player.PlayStatusChanged += status =>
            //{
            //    if (status == PlayStatus.Finished)
            //    {
            //        finished = true;
            //    }
            //};
            player.Play();
            var cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (true)
                {
                    var i = Console.ReadLine();
                    if (i == "q")
                    {
                        cts.Cancel();
                        return;
                    }
                }
            });

            while (!cts.IsCancellationRequested && player.PlayerStatus != PlayerStatus.Ready)
            {
                Console.WriteLine(player.PlayTime + "/" + player.TotalTime);
                Thread.Sleep(200);
            }

            player.Stop();
            await player.DisposeAsync();
            Console.WriteLine("done");

        }
    }
}
