#nullable enable

using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Services;

public interface IBeatmapActionService
{
    Task<Beatmap?> GetHighestDifficultyAsync(IMapIdentifiable? beatmap);
    Task OpenSourceFolderAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false);
    Task OpenScorePageAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false);
    Task SaveToCollectionAsync(IMapIdentifiable? beatmap);
    Task SaveToCollectionWithDifficultyPickerAsync(IMapIdentifiable? beatmap);
    Task ExportAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false);
    Task PlayAsync(IMapIdentifiable? beatmap, bool highestDifficulty = false, bool playInstantly = true);
    Task PlayWithDifficultyPickerAsync(IMapIdentifiable? beatmap);
}
