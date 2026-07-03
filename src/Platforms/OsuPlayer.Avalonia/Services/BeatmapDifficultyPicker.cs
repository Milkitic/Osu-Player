using System.Collections.Generic;
using System.Threading.Tasks;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Services;

public sealed class BeatmapDifficultyPicker : IBeatmapDifficultyPicker
{
    public async Task<Beatmap?> PickAsync(IReadOnlyList<Beatmap> beatmaps)
    {
        if (beatmaps.Count == 0)
        {
            return null;
        }

        return await FrontDialogService.ShowDifficultyPickerAsync(null, beatmaps);
    }
}
