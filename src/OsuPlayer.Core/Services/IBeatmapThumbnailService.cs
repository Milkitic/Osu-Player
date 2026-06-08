using System.Threading.Tasks;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Core.Services;

public interface IBeatmapThumbnailService
{
    Task<string> GetThumbByBeatmapDbIdAsync(BeatmapDataModel dataModel);
}
