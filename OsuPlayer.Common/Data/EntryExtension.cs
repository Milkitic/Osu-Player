using System;
using System.Collections.Generic;
using System.Linq;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Data;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;

namespace Milky.OsuPlayer.Common.Data
{
    public static class EntryExtension
    {
        public static MapIdentity GetIdentity(this BeatmapEntry entry) => entry != null ?
            new MapIdentity(entry.FolderName, entry.Version) : default;
        public static MapIdentity GetIdentity(this IMapIdentifiable entry) =>
            entry != null
                ? new MapIdentity(entry.FolderName, entry.Version)
                : default;

        public static IEnumerable<BeatmapEntry> ToBeatmapEntries(
            this IEnumerable<IMapIdentifiable> enumerable,
            IEnumerable<BeatmapEntry> baseEntries,
            bool playedOrAddedTime = true)
        {
            var identifiableListCopy = enumerable.ToList(); /*enumerable is List<IMapIdentifiable> list ? list : enumerable.ToList();*/
            bool flag = true;

            var db = new List<(BeatmapEntry entry, DateTime? lastPlayedTime, DateTime? addTime)>();
            baseEntries.Where(k => enumerable.Any(inner => inner.FolderName == k.FolderName));
            foreach (BeatmapEntry k in baseEntries)
            {
                foreach (var identifiable in identifiableListCopy)
                {
                    if (identifiable.FolderName != k.FolderName || identifiable.Version != k.Version)
                        continue;

                    if (identifiable is MapInfo mapInfo)
                        db.Add((k, mapInfo.LastPlayTime ?? new DateTime(), mapInfo.AddTime));
                    else
                    {
                        db.Add((k, null, null));
                        flag = false;
                    }

                    identifiableListCopy.Remove(identifiable);
                    break;
                }
            }

            if (flag)
            {
                return playedOrAddedTime
                    ? db.OrderByDescending(k => k.lastPlayedTime).Select(k => k.entry)
                    : db.OrderByDescending(k => k.addTime).Select(k => k.entry);
            }

            return db.Select(k => k.entry);
        }

        public static IEnumerable<BeatmapDataModel> ToDataModels(
            this IEnumerable<BeatmapEntry> entries,
            bool distinctByVersion = false)
        {
            return entries.Select((entry, i) =>
            {
                var model = new BeatmapDataModel
                {
                    Artist = entry.Artist,
                    ArtistUnicode = entry.ArtistUnicode,
                    BeatmapId = entry.BeatmapId,
                    Creator = entry.Creator,
                    FolderName = entry.FolderName,
                    GameMode = entry.GameMode,
                    SongSource = entry.SongSource,
                    SongTags = entry.SongTags,
                    Title = entry.Title,
                    TitleUnicode = entry.TitleUnicode,
                    Version = entry.Version,
                    BeatmapFileName = entry.BeatmapFileName,
                };
                try
                {
                    switch (entry.GameMode)
                    {
                        case GameMode.Standard:
                            model.Stars = Math.Round(entry.DiffStarRatingStandard[Mods.None], 2);
                            break;
                        case GameMode.Taiko:
                            model.Stars = Math.Round(entry.DiffStarRatingTaiko[Mods.None], 2);
                            break;
                        case GameMode.CatchTheBeat:
                            model.Stars = Math.Round(entry.DiffStarRatingCtB[Mods.None], 2);
                            break;
                        case GameMode.Mania:
                            model.Stars = Math.Round(entry.DiffStarRatingMania[Mods.None], 2);
                            break;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return model;
            }).Distinct(new DataModelComparer(distinctByVersion));
        }
        public static IEnumerable<BeatmapDataModel> ToDataModels(
            this IEnumerable<IMapIdentifiable> entry,
            IEnumerable<BeatmapEntry> baseEntries,
            bool distinctByVersion = false)
        {
            switch (entry)
            {
                case IEnumerable<BeatmapDataModel> dataModels:
                    return dataModels;
                case IEnumerable<MapInfo> infos:
                    return infos.ToBeatmapEntries(baseEntries).ToDataModels(distinctByVersion);

                default:
                    throw new ArgumentOutOfRangeException(nameof(entry));
            }
        }
    }
}
