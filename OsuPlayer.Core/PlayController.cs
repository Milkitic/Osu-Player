using Coosu.Beatmap;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using OsuPlayer.Audio;
using OsuPlayer.Data;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Core;

public class PlayController : IAsyncDisposable
{
    private readonly AsyncLock _asyncLock = new();
    private readonly AppSettings _settings;
    private string SongFolder => Path.Combine(_settings.Data.OsuBaseFolder ?? Constants.ApplicationDir, "Songs");
    public event Func<PlayItemContext, Task>? PlayerDisposing;

    public PlayController(AppSettings settings)
    {
        _settings = settings;
        if (!Directory.Exists(SongFolder))
        {
            Directory.CreateDirectory(SongFolder);
        }
    }

    public MemoryPlayList PlayList { get; } = new();
    public PlayItemContext? PlayItemContext { get; private set; }
    public OsuMixPlayer? Player { get; private set; }

    public async Task SwitchFile(string path, bool play)
    {
        using var _ = await _asyncLock.LockAsync();

        var standardizedPath = PathUtils.StandardizePath(path, SongFolder);

        //LogTo.Info($"Start load new song from path: {standardizedPath}");
        if (PlayItemContext?.PlayItem.Path.Equals(standardizedPath) == true)
        {
            //LogTo.Debug(() => "Same file to play, recreating...");
        }

        await ClearPlayer();
        PlayList.SetPointerByPath(standardizedPath, true);

        await using var dbContext = new ApplicationDbContext();

        var playItem = await dbContext.GetOrAddPlayItem(standardizedPath);
        var context = new PlayItemContext(playItem);
        await HandlePlayItem(context, path);
        dbContext.Update(context.PlayItem);
        await dbContext.SaveChangesAsync();

        if (PlayItemContext?.PlayItem.Folder != playItem.Folder)
        {
            CachedSoundFactory.ClearCacheSounds();
        }
    }

    private async Task HandlePlayItem(PlayItemContext context, string path)
    {
        if (path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
        {
            await HandlePlayItemOsu(context, path);
        }
    }

    private async Task HandlePlayItemOsu(PlayItemContext context, string path)
    {
        var coosu = await OsuFile.ReadFromFileAsync(path);
        context.PlayItem.LastPlay = DateTime.Now;
        var playItemDetail = context.PlayItem.PlayItemDetail;
        playItemDetail.Artist = coosu.Metadata.Artist;
        playItemDetail.ArtistUnicode = coosu.Metadata.ArtistUnicode ?? "";
        playItemDetail.AudioFileName = coosu.General.AudioFilename ?? "";
        playItemDetail.BeatmapFileName = Path.GetFileName(path);
        playItemDetail.BeatmapId = coosu.Metadata.BeatmapId;
        playItemDetail.BeatmapSetId = coosu.Metadata.BeatmapSetId;
        playItemDetail.Creator = coosu.Metadata.Creator ?? "";
        playItemDetail.Source = coosu.Metadata.Source ?? "";
        playItemDetail.Tags = string.Join(' ', coosu.Metadata.TagList);
        playItemDetail.Title = coosu.Metadata.Title;
        playItemDetail.TitleUnicode = coosu.Metadata.TitleUnicode ?? "";
        playItemDetail.Version = coosu.Metadata.Version ?? "";

        var player = new OsuMixPlayer(GetOptions(_settings, context.PlayItem.PlayItemConfig!), coosu);
        player.PlayStatusChanged += Player_PlayStatusChanged;
        player.PositionUpdated += Player_PositionUpdated;
    }

    private static PlayerOptions GetOptions(AppSettings settings, PlayItemConfig config)
    {
        var play = settings.Play;
        return new PlayerOptions(Constants.DefaultHitsoundDir)
        {
            InitialMainVolume = play.MainVolume,
            InitialMusicVolume = play.MusicVolume,
            InitialHitsoundVolume = play.AdditionVolume,
            InitialSampleVolume = play.SampleVolume,
            InitialHitsoundBalance = play.Balance,
            InitialOffset = config?.Offset ?? 0,
            InitialPlaybackRate = play.PlaybackRate,
            InitialKeepTune = play.IsTuneKept,
            DeviceDescription = play.DeviceDescription
        };
    }

    private async Task ClearPlayer()
    {
        if (PlayerDisposing != null && PlayItemContext != null)
        {
            await PlayerDisposing.Invoke(PlayItemContext);
        }

        if (Player != null)
        {
            Player.PlayStatusChanged -= Player_PlayStatusChanged;
            Player.PositionUpdated -= Player_PositionUpdated;
            await Player.DisposeAsync();
        }
    }

    private void Player_PlayStatusChanged(Milki.Extensions.MixPlayer.PlayStatus playStatus)
    {
    }

    private void Player_PositionUpdated(TimeSpan position)
    {
    }

    private async Task PlayNext(Direction direction, bool forceLoop)
    {
        var nextPath = PlayList.GetNextPath(direction, forceLoop);
        if (nextPath == null)
        {
            if (Player != null)
            {
                await Player.DisposeAsync();
            }

            Player = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ClearPlayer();
        _asyncLock.Dispose();
    }
}

public sealed class PlayItemContext
{
    public PlayItemContext(PlayItem playItem)
    {
        PlayItem = playItem;
    }

    public Dictionary<string, object> Tags { get; } = new();
    public bool IsFavorite { get; set; }
    public string? BackgroundPath { get; set; }
    public PlayItem PlayItem { get; }
}