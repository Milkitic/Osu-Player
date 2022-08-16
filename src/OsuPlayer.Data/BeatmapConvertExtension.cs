using System;
using System.IO;
using Coosu.Beatmap;
using Milki.OsuPlayer.Data.Models;

namespace Milki.OsuPlayer.Data
{
    public static class BeatmapConvertExtension
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static PlayItemDetail UpdateFromOSharp(this PlayItemDetail beatmap, LocalOsuFile osuFile)
        {
            beatmap.Artist = osuFile.Metadata?.Artist ?? "";
            beatmap.ArtistUnicode = osuFile.Metadata?.ArtistUnicode ?? "";
            beatmap.Title = osuFile.Metadata?.Title ?? "";
            beatmap.TitleUnicode = osuFile.Metadata?.TitleUnicode ?? "";
            beatmap.Creator = osuFile.Metadata?.Creator ?? "";
            beatmap.Version = osuFile.Metadata?.Version ?? "";

            beatmap.BeatmapFileName = Path.GetFileName(osuFile.OriginalPath)!;
            if (osuFile.HitObjects != null)
            {
                beatmap.TotalTime = TimeSpan.FromMilliseconds(osuFile.HitObjects.MaxTime);
            }

            beatmap.BeatmapId = osuFile.Metadata?.BeatmapId ?? -1;
            beatmap.BeatmapSetId = osuFile.Metadata?.BeatmapSetId ?? -1;
            beatmap.Source = osuFile.Metadata?.Source ?? "";
            beatmap.Tags = osuFile.Metadata == null ? "" : string.Join(" ", osuFile.Metadata.TagList);
            beatmap.AudioFileName = osuFile.General!.AudioFilename ?? "";

            throw new NotImplementedException("Determine whether osuFile is from song folder");
            beatmap.FolderName = Path.GetDirectoryName(osuFile.OriginalPath)!;
            return beatmap;
        }
    }
}