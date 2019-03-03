using System.Threading;
using System.Threading.Tasks;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Netease;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic;

namespace Milky.OsuPlayer.Media.Lyric.SourceProvider.Auto
{
    public class AutoSourceProvider : SourceProviderBase
    {
        private readonly SourceProviderBase[] _searchEngines;

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

        public AutoSourceProvider(bool strictMode) : base(strictMode)
        {
            _searchEngines = new SourceProviderBase[]
            {
                new NeteaseSourceProvider(StrictMode),
                new QqMusicSourceProvider(StrictMode),
                new KugouSourceProvider(StrictMode)
            };
        }
    }
}
