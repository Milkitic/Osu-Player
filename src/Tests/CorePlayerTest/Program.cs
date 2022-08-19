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

namespace CorePlayerTest;

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
        //var path = "F:\\milkitic\\Songs\\" +
        //           "1376486 Risshuu feat. Choko - Take\\" +
        //           "Risshuu feat. Choko - Take (yf_bmp) [Ta~ke take take take take take tatata~].osu";
        var path =
            @"C:\Users\milkitic\AppData\Local\osu!\Songs\cYsmix_triangles\cYsmix - triangles (yf_bmp) [Expert].osu";
        var folder = Path.GetDirectoryName(path);
        var osuFile = await OsuFile.ReadFromFileAsync(path);
        var engine = new AudioPlaybackEngine(new DeviceDescription()
        {
            WavePlayerType = WavePlayerType.WASAPI,
            FriendlyName = null,
            IsExclusive = false,
            Latency = 1
        });
        var player = new OsuMixPlayer(osuFile, engine)
        {
            Volume = 0.05f,
            Offset = 35
        };
        await player.InitializeAsync();

        bool finished = false;
        //player.PlayStatusChanged += status =>
        //{
        //    if (status == PlayStatus.Finished)
        //    {
        //        finished = true;
        //    }
        //};

        var cts = new CancellationTokenSource();
        Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                Console.WriteLine($"{player.PlayTime:mm\\:ss\\.fff}/{player.TotalTime:mm\\:ss\\.fff}");
                Thread.Sleep(200);
            }
        });

        player.Play();
        await Task.Delay(4000);

        player.Pause();
        await Task.Delay(1000);
        player.Seek(TimeSpan.FromSeconds(30));
        player.Play();

        while (true)
        {
            var consoleKeyInfo = Console.ReadKey(true);
            if (consoleKeyInfo.KeyChar == 'q')
            {
                cts.Cancel();
                break;
            }
        }

        player.Stop();
        await player.DisposeAsync();
        Console.WriteLine("done");

    }
}