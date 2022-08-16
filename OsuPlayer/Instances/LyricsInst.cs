using System;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Media.Lyric;
using Milki.OsuPlayer.Media.Lyric.SourceProvider;
using Milki.OsuPlayer.Media.Lyric.SourceProvider.Auto;
using Milki.OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using Milki.OsuPlayer.Media.Lyric.SourceProvider.Netease;
using Milki.OsuPlayer.Media.Lyric.SourceProvider.QQMusic;

namespace Milki.OsuPlayer.Instances
{
    public class LyricsInst
    {
        public LyricProvider LyricProvider { get; private set; }

        public void ReloadLyricProvider(bool useStrict = true)
        {
            AppSettings.Default.LyricSection.StrictMode = useStrict;
            Settings.StrictMatch = useStrict;
            SourceProviderBase provider;
            switch (AppSettings.Default.LyricSection.LyricSource)
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
                    throw new ArgumentOutOfRangeException(nameof(AppSettings.Default.LyricSection.LyricSource),
                        AppSettings.Default.LyricSection.LyricSource, null);
            }

            LyricProvider = new LyricProvider(provider, LyricProvideType.Original);
        }
    }
}