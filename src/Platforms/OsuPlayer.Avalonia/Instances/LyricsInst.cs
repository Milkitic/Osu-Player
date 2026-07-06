using System;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Media.Lyric;
using OsuPlayer.Media.Lyric.SourceProvider;
using OsuPlayer.Media.Lyric.SourceProvider.Auto;
using OsuPlayer.Media.Lyric.SourceProvider.Kugou;
using OsuPlayer.Media.Lyric.SourceProvider.Netease;
using OsuPlayer.Media.Lyric.SourceProvider.QQMusic;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Instances;

public class LyricsInst
{
    public LyricProvider? LyricProvider { get; private set; }

    public void ReloadLyricProvider(bool? useStrict = null)
    {
        var lyricSettings = AppSettings.Default?.Lyric ?? new LyricSection();
        if (useStrict.HasValue)
        {
            lyricSettings.StrictMode = useStrict.Value;
        }

        Settings.StrictMatch = lyricSettings.StrictMode;
        SourceProviderBase provider;
        switch (lyricSettings.LyricSource)
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
                throw new ArgumentOutOfRangeException(nameof(lyricSettings.LyricSource),
                    lyricSettings.LyricSource, null);
        }

        LyricProvider = new LyricProvider(provider, lyricSettings.ProvideType);
    }
}
