#nullable enable

using Coosu.Beatmap;
using LyricsFinder;
using LyricsFinder.SourcePrivoder.Auto;
using LyricsFinder.SourcePrivoder.Kugou;
using LyricsFinder.SourcePrivoder.QQMusic;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Services;

public class LyricsService : IDisposable
{
    private class LyricProvider
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

        private async Task<Lyrics> InnerGetLyric(string artist, string title, int duration, bool useTranslated,
            bool useCache = false)
        {
            if (useCache && TryGetCache(title, artist, duration, useTranslated, out Lyrics cached))
            {
                return cached;
            }

            Lyrics lyric =
                await _sourceProvider.ProvideLyricAsync(artist, title, duration, useTranslated, CancellationToken.None);
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

    private LyricWindow _lyricWindow = null!;
    private readonly PlayerService _playerService;
    private LyricProvider? _lyricProvider;
    private Task? _searchLyricTask;

    public LyricsService(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public async Task CreateWindowAsync()
    {
        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            _lyricWindow = new LyricWindow();
        });
    }

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

        _lyricProvider = new LyricProvider(provider, LyricProvideType.Original);
    }

    /// <summary>
    /// Call lyric provider to check lyric
    /// </summary>
    public void SetLyricSynchronously(PlayItem? playItem)
    {
        Task.Run(async () =>
        {
            if (_searchLyricTask?.IsTaskBusy() == true)
            {
                await _searchLyricTask;
            }

            _searchLyricTask = Task.Run(async () =>
            {
                if (_playerService.LastLoadContext?.PlayItem == null) return;
                if (_playerService.ActiveMixPlayer == null) return;

                var playItemDetail = playItem?.PlayItemDetail ?? _playerService.LastLoadContext.PlayItem.PlayItemDetail;

                var artistUnicode = playItemDetail.ArtistUnicode;
                var titleUnicode = playItemDetail.TitleUnicode;

                var metaArtist = new MetaString(playItemDetail.Artist, artistUnicode);
                var metaTitle = new MetaString(playItemDetail.Tags, titleUnicode);

                var lyric = await _lyricProvider.GetLyricAsync(artistUnicode, titleUnicode,
                    (int)_playerService.ActiveMixPlayer.MusicTrack.Duration);
                _lyricWindow.SetNewLyric(lyric, metaArtist, metaTitle);
                _lyricWindow.StartWork();
            });
        });
    }

    public void Dispose()
    {
        _lyricWindow?.Dispose();
    }
}