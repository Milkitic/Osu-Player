using System;
using System.Collections.Generic;
using System.Linq;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Data.EF.Model.V1;
using Milky.OsuPlayer.Common.Metadata;
using OSharp.Beatmap.MetaData;
using OSharp.Beatmap.Sections.GamePlay;

namespace Milky.OsuPlayer.Common.Data
{
    public static class BeatmapExtension
    {
        public static IEnumerable<Beatmap> ToBeatmapEntries(
            this IEnumerable<IMapIdentifiable> enumerable,
            bool playedOrAddedTime = true)
        {
            if (enumerable is IEnumerable<Beatmap> foo)
                return foo;

            throw new NotImplementedException();
        }

        public static IEnumerable<BeatmapDataModel> ToDataModels(
            this IEnumerable<Beatmap> entries,
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
                    OSharpGameMode = entry.GameMode,
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
                        case GameMode.Circle:
                            model.Stars = Math.Round(entry.DiffSrNoneStandard, 2);
                            break;
                        case GameMode.Taiko:
                            model.Stars = Math.Round(entry.DiffSrNoneTaiko, 2);
                            break;
                        case GameMode.Catch:
                            model.Stars = Math.Round(entry.DiffSrNoneCtB, 2);
                            break;
                        case GameMode.Mania:
                            model.Stars = Math.Round(entry.DiffSrNoneMania, 2);
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
    }
}
