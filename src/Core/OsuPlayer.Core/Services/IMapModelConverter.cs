using System.Collections.Generic;
using System.Threading.Tasks;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Core.Services;

public interface IMapModelConverter
{
    List<BeatmapDataModel> ToDataModelList(IEnumerable<IMapIdentifiable> identifiable, bool distinctByVersion = false);
    Task<List<BeatmapDataModel>> ToDataModelListAsync(IEnumerable<IMapIdentifiable> identifiable, bool distinctByVersion = false);
}
