using System.Threading.Tasks;
using Milki.OsuPlayer.Media.Lyric.Models;

namespace Milki.OsuPlayer.Media.Lyric.SourceProvider.Auto
{
    [SourceProviderName("auto", "DarkProjector")]
    public class AutoSourceProvider : SourceProviderBase
    {
        public readonly SourceProviderBase[] SearchEngines;

        public AutoSourceProvider(SourceProviderBase[] sourceProviders)
        {
            //search_engines=SourceProviderManager.LyricsSourceProvidersType
            //    .Select(x => x.GetCustomAttribute<SourceProviderNameAttribute>())
            //    .OfType<SourceProviderNameAttribute>()
            //    .Where(x => !x.Name.Equals("auto", StringComparison.InvariantCultureIgnoreCase))
            //    .Select(x => SourceProviderManager.GetOrCreateSourceProvier(x.Name))
            //    .ToArray();
            SearchEngines = sourceProviders;
        }

        public override Lyrics ProvideLyric(string artist, string title, int time, bool request_trans_lyrics)
        {
            Task<Lyrics>[] tasks = new Task<Lyrics>[SearchEngines.Length];

            System.Threading.CancellationTokenSource token = new System.Threading.CancellationTokenSource();

            for (int i = 0; i < SearchEngines.Length; i++)
                tasks[i] = Task.Factory.StartNew<Lyrics>(
                    index => SearchEngines[(int)index].ProvideLyric(artist, title, time, request_trans_lyrics),
                    i,
                    token.Token);

            Lyrics lyrics = null;

            for (int i = 0; i < SearchEngines.Length; i++)
            {
                lyrics = tasks[i].Result;

                //如果是刚好是要相同版本的歌词那可以直接返回了,否则就等一下其他源是不是还能拿到合适的版本
                if (lyrics != null)
                {
                    token.Cancel();
                    Utils.Debug($"Quick select lyric from {SearchEngines[i].GetType().Name}");
                    break;
                }
            }

            return lyrics;
        }
    }
}
