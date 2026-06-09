#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Services;

public interface IBeatmapDifficultyPicker
{
    Task<Beatmap?> PickAsync(IReadOnlyList<Beatmap> beatmaps);
}
