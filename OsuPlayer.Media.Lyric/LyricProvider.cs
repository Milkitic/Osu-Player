using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Media.Lyric.SourceProvider;
using System;
using Milky.OsuPlayer.Media.Lyric.Models;

namespace Milky.OsuPlayer.Media.Lyric
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

        public Lyrics GetLyric(string artist, string title, int duration)
        {
            Lyrics lyric;
            switch (ProvideType)
            {
                case LyricProvideType.PreferBoth:
                    var transLyrics = InnerGetLyric(artist, title, duration, true);
                    var rawLyrics = InnerGetLyric(artist, title, duration, false);
                    Console.WriteLine(@"翻译歌词: {0}, 原歌词: {1}.", transLyrics != null, rawLyrics != null);
                    lyric = rawLyrics + transLyrics;
                    break;
                default:
                    lyric = InnerGetLyric(artist, title, duration, false);
                    if (ProvideType == LyricProvideType.PreferTranslated)
                    {
                        var tmp = InnerGetLyric(artist, title, duration, true);
                        if (tmp != null)
                            lyric = tmp;
                    }
                    break;
            }

            return lyric;
        }

        private Lyrics InnerGetLyric(string artist, string title, int duration, bool useTranslated, bool useCache = false)
        {
            if (useCache && TryGetCache(title, artist, duration, useTranslated, out Lyrics cached))
            {
                return cached;
            }

            Lyrics lyric = _sourceProvider?.ProvideLyric(artist, title, duration, useTranslated);
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
