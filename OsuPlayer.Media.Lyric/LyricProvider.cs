using System;
using System.Threading.Tasks;
using Milki.OsuPlayer.Media.Lyric.Models;
using Milki.OsuPlayer.Media.Lyric.SourceProvider;

namespace Milki.OsuPlayer.Media.Lyric
{
    public class LyricProvider
    {
        public LyricProvideType ProvideType { get; set; }

        private readonly SourceProviderBase _sourceProvider;

        public LyricProvider(SourceProviderBase provider, LyricProvideType provideType)
        {
            _sourceProvider = provider;
            ProvideType = provideType;
        }

        public async Task<Lyrics> GetLyricAsync(string artist, string title, int duration)
        {
            Lyrics lyric;
            switch (ProvideType)
            {
                case LyricProvideType.PreferBoth:
                    var transLyrics = await InnerGetLyric(artist, title, duration, true);
                    var rawLyrics = await InnerGetLyric(artist, title, duration, false);
                    Console.WriteLine(@"翻译歌词: {0}, 原歌词: {1}.", transLyrics != null, rawLyrics != null);
                    lyric = rawLyrics + transLyrics;
                    break;
                default:
                    lyric = await InnerGetLyric(artist, title, duration, false);
                    if (ProvideType == LyricProvideType.PreferTranslated)
                    {
                        var tmp = await InnerGetLyric(artist, title, duration, true);
                        if (tmp != null)
                            lyric = tmp;
                    }
                    break;
            }

            return lyric;
        }

        private async Task<Lyrics> InnerGetLyric(string artist, string title, int duration, bool useTranslated, bool useCache = false)
        {
            if (useCache && TryGetCache(title, artist, duration, useTranslated, out Lyrics cached))
            {
                return cached;
            }

            Lyrics lyric = await Task.Run(() => _sourceProvider?.ProvideLyric(artist, title, duration, useTranslated));

            if (useCache) WriteCache(title, artist, duration, lyric);
            return lyric;
        }

        private static void WriteCache(string title, string artist, int duration, Lyrics lyric)
        {
            throw new NotImplementedException();
        }

        private static bool TryGetCache(string title, string artist, int duration, bool useTranslated, out Lyrics lyric)
        {
            throw new NotImplementedException();
        }
    }
}
