#nullable enable

using Coosu.Beatmap;
using LyricsFinder;
using LyricsFinder.SourcePrivoder.Auto;
using LyricsFinder.SourcePrivoder.Kugou;
using LyricsFinder.SourcePrivoder.QQMusic;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.LyricsFinder;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Services;

public class LyricsService : IDisposable
{
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