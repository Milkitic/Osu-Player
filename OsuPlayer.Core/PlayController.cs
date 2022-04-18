using Anotar.NLog;
using Coosu.Beatmap;
using OsuPlayer.Audio;
using OsuPlayer.Data;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Core;

public class PlayController
{
    private readonly string _songFolder;
    public event Func<PlayItemContext, Task>? PlayerDisposing;

    public PlayController(string songFolder)
    {
        _songFolder = songFolder;
    }

    public MemoryPlayList PlayList { get; } = new();
    public PlayItemContext? PlayItemContext { get; private set; }
    public OsuMixPlayer? Player { get; private set; }

    public async Task SwitchFile(string path, bool play)
    {
        var standardizedPath = PathUtils.StandardizePath(path, _songFolder);

        //LogTo.Info($"Start load new song from path: {standardizedPath}");
        if (PlayItemContext?.PlayItem.Path.Equals(standardizedPath) == true)
        {
            //LogTo.Debug(() => "Same file to play, recreating...");
        }

        await ClearPlayer();

        await using var dbContext = new ApplicationDbContext();

        var playItem = await dbContext.GetOrAddPlayItem(standardizedPath);
        var context = new PlayItemContext(playItem);
        await HandlePlayItem(context, path);
        dbContext.Update(context.PlayItem);
        await dbContext.SaveChangesAsync();
        var pointer = PlayList.SetPointerByPath(standardizedPath, true);
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
    }

    private async Task ClearPlayer()
    {
        if (PlayerDisposing != null && PlayItemContext != null)
        {
            await PlayerDisposing.Invoke(PlayItemContext);
        }
        if (Player != null)
        {
            await Player.DisposeAsync();
        }
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
}

public sealed class PlayItemContext
{
    public PlayItemContext(PlayItem playItem)
    {
        PlayItem = playItem;
    }

    public string? BackgroundPath { get; set; }
    public PlayItem PlayItem { get; }
}