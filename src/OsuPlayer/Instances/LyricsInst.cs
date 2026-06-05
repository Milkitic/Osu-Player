using System;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Lyric;
using OsuPlayer.Media.Lyric.SourceProvider;
using OsuPlayer.Media.Lyric.SourceProvider.Auto;
using OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using OsuPlayer.Media.Lyric.SourceProvider.Netease;
using OsuPlayer.Media.Lyric.SourceProvider.QQMusic;

namespace OsuPlayer.Instances
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
                    throw new ArgumentOutOfRangeException(nameof(AppSettings.Default.Lyric.LyricSource),
                        AppSettings.Default.Lyric.LyricSource, null);
            }

            LyricProvider = new LyricProvider(provider, LyricProvideType.Original);
        }
    }
}