using System;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Media.Lyric;
using Milky.OsuPlayer.Media.Lyric.SourceProvider;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Auto;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.Netease;
using Milky.OsuPlayer.Media.Lyric.SourceProvider.QQMusic;

namespace Milky.OsuPlayer.Instances
{
    public class LyricsInst
    {
        public LyricProvider LyricProvider { get; private set; }

        public void ReloadLyricProvider(bool useStrict = true)
        {
            AppSettings.Default.Lyric.StrictMode = useStrict;
            Settings.StrictMatch = useStrict;
            SourceProviderBase provider;
            switch (AppSettings.Default.Lyric.LyricSource)
            {
                case LyricSource.Auto:
                    provider = new AutoSourceProvider(new SourceProviderBase[]
                    {
                        new NeteaseSourceProvider(),
                        new KugouSourceProvider(),
                        new QQMusicSourceProvider()
                    });
                    break;
                case LyricSource.Netease:
                    provider = new NeteaseSourceProvider();
                    break;
                case LyricSource.Kugou:
                    provider = new KugouSourceProvider();
                    break;
                case LyricSource.QqMusic:
                    provider = new QQMusicSourceProvider();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LyricProvider = new LyricProvider(provider, LyricProvideType.Original);
        }
    }
}