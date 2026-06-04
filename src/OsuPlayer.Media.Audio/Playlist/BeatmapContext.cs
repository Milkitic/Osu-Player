using System.Threading.Tasks;
using Coosu.Beatmap;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Media.Audio.Playlist;

/// <summary>
/// Anemic model holding the currently-selected beatmap plus all of its
/// derived metadata. Knows nothing about playback — call sites that need
/// to drive the player obtain one from the controller that produced this
/// context.
/// </summary>
public class BeatmapContext
{
    public BeatmapContext() : this(new Beatmap()) { }

    private BeatmapContext(Beatmap beatmap)
    {
        Beatmap = beatmap;
        BeatmapDetail = new BeatmapDetail(beatmap);
    }

    public static async Task<BeatmapContext> CreateAsync(Beatmap beatmap, IPlayerDataStore playerData)
    {
        return new BeatmapContext(beatmap)
        {
            BeatmapSettings = await playerData.GetMapFromDbAsync(beatmap.GetIdentity()),
        };
    }

    public bool FullLoaded { get; set; } = false;
    public Beatmap Beatmap { get; }
    public BeatmapSettings? BeatmapSettings { get; private set; }
    public BeatmapDetail BeatmapDetail { get; }
    public LocalOsuFile? OsuFile { get; set; }
    public bool PlayInstantly { get; set; }

    public static bool operator ==(BeatmapContext? bc1, BeatmapContext? bc2)
    {
        return Equals(bc1, bc2);
    }

    public static bool operator !=(BeatmapContext? bc1, BeatmapContext? bc2)
    {
        return !(bc1 == bc2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not BeatmapContext bc) return false;
        return Equals(bc);
    }

    protected bool Equals(BeatmapContext other)
    {
        return Equals(Beatmap, other.Beatmap);
    }

    public override int GetHashCode()
    {
        return Beatmap != null ? Beatmap.GetHashCode() : 0;
    }
}
