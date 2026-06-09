using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Core.Services;

public class MapModelConverter : IMapModelConverter
{
    private readonly ILogger<MapModelConverter> _logger;
    private readonly IPlayerDataStore _playerData;

    public MapModelConverter(IPlayerDataStore playerData, ILogger<MapModelConverter> logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    public List<BeatmapDataModel> ToDataModelList(IEnumerable<IMapIdentifiable> identifiable, bool distinctByVersion = false)
    {
        List<BeatmapDataModel> ret;
        switch (identifiable)
        {
            case ObservableCollection<Beatmap> beatmaps1:
                ret = InnerToDataModelList(beatmaps1);
                break;
            case List<Beatmap> beatmaps:
                ret = InnerToDataModelList(beatmaps);
                break;
            case ObservableCollection<BeatmapDataModel> dataModels1:
                ret = dataModels1.ToList();
                break;
            case List<BeatmapDataModel> dataModels:
                ret = dataModels;
                break;
            case List<BeatmapSettings>:
                throw new InvalidOperationException("Use ToDataModelListAsync for BeatmapSettings sources.");
            default:
                throw new ArgumentOutOfRangeException(nameof(identifiable), identifiable?.GetType(),
                    "Not support source type.");
        }

        return ret.Distinct(new DataModelComparer(distinctByVersion)).ToList();
    }

    public async Task<List<BeatmapDataModel>> ToDataModelListAsync(IEnumerable<IMapIdentifiable> identifiable, bool distinctByVersion = false)
    {
        if (identifiable is List<BeatmapSettings> infos)
        {
            var beatmaps = await _playerData.GetBeatmapsByIdentifiableAsync(infos);
            return InnerToDataModelList(beatmaps).Distinct(new DataModelComparer(distinctByVersion)).ToList();
        }

        return ToDataModelList(identifiable, distinctByVersion);
    }

    private List<BeatmapDataModel> InnerToDataModelList(IEnumerable<Beatmap> beatmaps)
    {
        return beatmaps.Select((beatmap, i) =>
        {
            var model = new BeatmapDataModel
            {
                Artist = beatmap.Artist,
                ArtistUnicode = beatmap.ArtistUnicode,
                BeatmapId = beatmap.BeatmapId,
                Creator = beatmap.Creator,
                FolderName = beatmap.FolderName,
                GameMode = beatmap.GameMode,
                SongSource = beatmap.SongSource,
                SongTags = beatmap.SongTags,
                Title = beatmap.Title,
                TitleUnicode = beatmap.TitleUnicode,
                Version = beatmap.Version,
                BeatmapFileName = beatmap.BeatmapFileName,
                InOwnDb = beatmap.InOwnDb,
                BeatmapDbId = beatmap.Id
            };
            try
            {
                switch (beatmap.GameMode)
                {
                    case Coosu.Beatmap.Sections.GamePlay.GameMode.Circle:
                        model.Stars = Math.Round(beatmap.DiffSrNoneStandard, 2);
                        break;
                    case Coosu.Beatmap.Sections.GamePlay.GameMode.Taiko:
                        model.Stars = Math.Round(beatmap.DiffSrNoneTaiko, 2);
                        break;
                    case Coosu.Beatmap.Sections.GamePlay.GameMode.Catch:
                        model.Stars = Math.Round(beatmap.DiffSrNoneCtB, 2);
                        break;
                    case Coosu.Beatmap.Sections.GamePlay.GameMode.Mania:
                        model.Stars = Math.Round(beatmap.DiffSrNoneMania, 2);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while mapping beatmap star rating");
            }

            return model;
        }).ToList();
    }
}
