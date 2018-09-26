using System.Threading;
using System.Threading.Tasks;
using Milkitic.OsuPlayer.Wpf.LyricExtension.Model;
using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Base;
using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Kugou;
using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Netease;
using Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.QQMusic;

namespace Milkitic.OsuPlayer.Wpf.LyricExtension.SourcePrivoder.Auto
{
    public class AutoSourceProvider : SourceProviderBase
    {
        private readonly SourceProviderBase[] _searchEngines =
        {
            new NeteaseSourceProvider(),
            new QqMusicSourceProvider(),
            new KugouSourceProvider()
        };

        public override Lyric ProvideLyric(string artist, string title, int time, bool requestTransLyrics)
        {
            Task<Lyric>[] tasks = new Task<Lyric>[_searchEngines.Length];

            CancellationTokenSource cts = new CancellationTokenSource();

            for (int i = 0; i < _searchEngines.Length; i++)
            {
                int j = i;
                tasks[i] = Task.Run(() => _searchEngines[j].ProvideLyric(artist, title, time, requestTransLyrics),
                    cts.Token);
            }

            Lyric lyric = null;

            for (int i = 0; i < _searchEngines.Length; i++)
            {
                lyric = tasks[i].Result;

                //如果是刚好是要相同版本的歌词那可以直接返回了,否则就等一下其他源是不是还能拿到合适的版本
                if (lyric != null)
                {
                    cts.Cancel();
                    break;
                }
            }

            return lyric;
        }
    }
}
