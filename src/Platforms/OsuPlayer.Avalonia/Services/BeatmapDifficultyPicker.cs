using System.Collections.Generic;
using System.Threading.Tasks;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Services;

public sealed class BeatmapDifficultyPicker : IBeatmapDifficultyPicker
{
    private readonly IAppNotificationService _notifications;

    public BeatmapDifficultyPicker(IAppNotificationService notifications)
    {
        _notifications = notifications;
    }

    public Task<Beatmap?> PickAsync(IReadOnlyList<Beatmap> beatmaps)
    {
        if (beatmaps.Count == 0)
            return Task.FromResult<Beatmap?>(null);

        var highest = beatmaps[beatmaps.Count - 1];
        _notifications.Push("Difficulty picker dialog is not yet implemented; selecting highest difficulty.");
        return Task.FromResult<Beatmap?>(highest);
    }
}
