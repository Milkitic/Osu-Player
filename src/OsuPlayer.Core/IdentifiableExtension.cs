using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coosu.Beatmap.MetaData;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Services;

namespace Milky.OsuPlayer.Core
{
    public static class IdentifiableExtension
    {
        private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly IPlayerDataStore s_playerData = new PlayerDataService();

        public static List<BeatmapDataModel> ToDataModelList(this IEnumerable<IMapIdentifiable> identifiable,
            bool distinctByVersion = false)
        {
            List<BeatmapDataModel> ret;
            switch (identifiable)
            {
                case ObservableCollection<Beatmap> beatmaps1:
                    ret = beatmaps1.InnerToDataModelList();
                    break;
                case List<Beatmap> beatmaps:
                    ret = beatmaps.InnerToDataModelList();
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

        public static async Task<List<BeatmapDataModel>> ToDataModelListAsync(
            this IEnumerable<IMapIdentifiable> identifiable,
            bool distinctByVersion = false)
        {
            if (identifiable is List<BeatmapSettings> infos)
            {
                var beatmaps = await s_playerData.GetBeatmapsByIdentifiableAsync(infos);
                return beatmaps.InnerToDataModelList().Distinct(new DataModelComparer(distinctByVersion)).ToList();
            }

            return identifiable.ToDataModelList(distinctByVersion);
        }

        private static List<BeatmapDataModel> InnerToDataModelList(this IEnumerable<Beatmap> beatmaps)
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
                    s_logger.Error(ex);
                }

                return model;
            }).ToList();
        }

        public static string GetFolder(this IMapIdentifiable map, out bool isFromDb, out string freePath)
        {
            if (map.IsMapTemporary())
            {
                var folder = Path.GetDirectoryName(map.FolderName);
                isFromDb = false;
                freePath = map.FolderName;
                return folder;
            }

            isFromDb = true;
            freePath = null;
            return map.InOwnDb
                ? Path.Combine(Domain.CustomSongPath, map.FolderName)
                : Path.Combine(Domain.OsuSongPath, map.FolderName);
        }
    }
}
