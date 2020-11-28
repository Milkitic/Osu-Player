using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using OSharp.Beatmap.MetaData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Milky.OsuPlayer.Common
{
    public static class IdentifiableExtension
    {
        private static AppDbOperator _beatmapDbOperator = new AppDbOperator();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static List<BeatmapDataModel> ToDataModelList(this IEnumerable<IMapIdentifiable> identifiable, bool distinctByVersion = false)
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
                case List<BeatmapSettings> infos:
                    ret = _beatmapDbOperator.GetBeatmapsByIdentifiable(infos).InnerToDataModelList();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identifiable), identifiable?.GetType(),
                        "Not support source type.");
            }

            return ret.Distinct(new DataModelComparer(distinctByVersion)).ToList();
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
                    FolderNameOrPath = beatmap.FolderNameOrPath,
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
                        case OSharp.Beatmap.Sections.GamePlay.GameMode.Circle:
                            model.Stars = Math.Round(beatmap.DiffSrNoneStandard, 2);
                            break;
                        case OSharp.Beatmap.Sections.GamePlay.GameMode.Taiko:
                            model.Stars = Math.Round(beatmap.DiffSrNoneTaiko, 2);
                            break;
                        case OSharp.Beatmap.Sections.GamePlay.GameMode.Catch:
                            model.Stars = Math.Round(beatmap.DiffSrNoneCtB, 2);
                            break;
                        case OSharp.Beatmap.Sections.GamePlay.GameMode.Mania:
                            model.Stars = Math.Round(beatmap.DiffSrNoneMania, 2);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                return model;
            }).ToList();
        }

        public static bool TryGetValue<T>(this HashSet<T> hs, Func<T, bool> predicate, out IEnumerable<T> actualValues)
        {
            actualValues = hs.Where(predicate);
            if (actualValues.Any())
            {
                return true;
            }

            actualValues = null;
            return false;
        }

        public static bool TryGetValue<T>(this HashSet<T> hs, T equalValue, out T actualValue)
        {
            if (hs.Contains(equalValue))
            {
                actualValue = hs.First(k => k.Equals(equalValue));
                return true;
            }

            actualValue = default;
            return false;
        }

        public static string GetFolder(this IMapIdentifiable map, out bool isFromDb, out string freePath)
        {
            if (map.IsMapTemporary())
            {
                var folder = Path.GetDirectoryName(map.FolderNameOrPath);
                isFromDb = false;
                freePath = map.FolderNameOrPath;
                return folder;
            }

            isFromDb = true;
            freePath = null;
            return map.InOwnDb
                ? Path.Combine(Domain.CustomSongPath, map.FolderNameOrPath)
                : Path.Combine(Domain.OsuSongPath, map.FolderNameOrPath);
        }
    }
}
