using System;
using LyricsFinder;
using LyricsFinder.SourcePrivoder.Auto;
using LyricsFinder.SourcePrivoder.Kugou;
using LyricsFinder.SourcePrivoder.QQMusic;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.LyricsFinder;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Services;

public class LyricsService
{
    public LyricProvider LyricProvider { get; private set; }

    public void ReloadLyricProvider(bool useStrict = true)
    {
        AppSettings.Default.LyricSection.StrictMode = useStrict;
        GlobalSetting.StrictMatch = useStrict;
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