using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Milky.OsuPlayer.Common.Data.EF;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using osu_database_reader.Components.Beatmaps;
using OSharp.Beatmap.MetaData;

namespace Milky.OsuPlayer.Common.Data
{
    public static class IdentifiableExtension
    {
        private static EF.BeatmapDbOperator _beatmapDbOperator = new EF.BeatmapDbOperator();
        public static bool EqualsTo(this IMapIdentifiable id1, IMapIdentifiable id2) =>
            id1.FolderName == id2.FolderName && id1.Version == id2.Version;

        public static MapIdentity GetIdentity(this BeatmapEntry entry) => entry != null ?
            new MapIdentity(entry.FolderName, entry.Version) : default;
        public static MapIdentity GetIdentity(this IMapIdentifiable identifiable) =>
            identifiable != null
                ? new MapIdentity(identifiable.FolderName, identifiable.Version)
                : default;

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
                case List<MapInfo> infos:
                    ret = _beatmapDbOperator.GetBeatmapsByIdentifiable(infos).InnerToDataModelList();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(identifiable));
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
                    FolderName = beatmap.FolderName,
                    OSharpGameMode = beatmap.GameMode,
                    SongSource = beatmap.SongSource,
                    SongTags = beatmap.SongTags,
                    Title = beatmap.Title,
                    TitleUnicode = beatmap.TitleUnicode,
                    Version = beatmap.Version,
                    BeatmapFileName = beatmap.BeatmapFileName,
                    InOwnDb = beatmap.InOwnFolder
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
                catch (Exception)
                {
                    // ignored
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
    }
}
