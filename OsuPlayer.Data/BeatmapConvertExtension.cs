using System;
using Milky.OsuPlayer.Data.Models;
using OSharp.Beatmap;
using osu.Shared;
using osu_database_reader.Components.Beatmaps;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Milky.OsuPlayer.Shared.Models.NostModels;
using OSharpGameMode = OSharp.Beatmap.Sections.GamePlay.GameMode;

namespace Milky.OsuPlayer.Data
{
    public static class BeatmapConvertExtension
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static Beatmap UpdateFromHolly(this Beatmap beatmap, BeatmapEntry entry)
        {
            beatmap.Artist = entry.Artist;
            beatmap.ArtistUnicode = entry.ArtistUnicode;
            beatmap.Title = entry.Title;
            beatmap.TitleUnicode = entry.TitleUnicode;
            beatmap.Creator = entry.Creator;
            beatmap.Version = entry.Version;
            beatmap.BeatmapFileName = entry.BeatmapFileName;
            beatmap.LastModifiedTime = entry.LastModifiedTime;
            beatmap.DiffSrNoneStandard = entry.DiffStarRatingStandard.ContainsKey(Mods.None)
                  ? entry.DiffStarRatingStandard[Mods.None]
                  : -1;
            beatmap.DiffSrNoneTaiko = entry.DiffStarRatingTaiko.ContainsKey(Mods.None)
                  ? entry.DiffStarRatingTaiko[Mods.None]
                  : -1;
            beatmap.DiffSrNoneCtB = entry.DiffStarRatingCtB.ContainsKey(Mods.None) ? entry.DiffStarRatingCtB[Mods.None] : -1;
            beatmap.DiffSrNoneMania = entry.DiffStarRatingMania.ContainsKey(Mods.None)
                  ? entry.DiffStarRatingMania[Mods.None]
                  : -1;
            beatmap.DrainTimeSeconds = entry.DrainTimeSeconds;
            beatmap.TotalTime = entry.TotalTime;
            beatmap.AudioPreviewTime = entry.AudioPreviewTime;
            beatmap.BeatmapId = entry.BeatmapId;
            beatmap.BeatmapSetId = entry.BeatmapSetId;
            beatmap.GameMode = entry.GameMode.ParseHollyToOSharp();
            beatmap.SongSource = entry.SongSource;
            beatmap.SongTags = entry.SongTags;
            beatmap.FolderNameOrPath = entry.FolderName?.TrimEnd();
            beatmap.AudioFileName = entry.AudioFileName;

            beatmap.Id = Zip($"!{beatmap.FolderNameOrPath}|{beatmap.Version}|{beatmap.InOwnDb}");
            return beatmap;
        }

        public static Beatmap ParseFromHolly(BeatmapEntry entry)
        {
            return (new Beatmap()).UpdateFromHolly(entry);
        }

        public static Beatmap UpdateFromOSharp(this Beatmap beatmap, OsuFile osuFile)
        {
            beatmap.Artist = osuFile.Metadata.Artist;
            beatmap.ArtistUnicode = osuFile.Metadata.ArtistUnicode;
            beatmap.Title = osuFile.Metadata.Title;
            beatmap.TitleUnicode = osuFile.Metadata.TitleUnicode;
            beatmap.Creator = osuFile.Metadata.Creator;
            beatmap.Version = osuFile.Metadata.Version;
            //BeatmapFileName = osuFile.BeatmapFileName;
            //LastModifiedTime = osuFile.LastModifiedTime;
            //DiffSrNoneStandard = osuFile.DiffStarRatingStandard.ContainsKey(Mods.None)
            //    ? osuFile.DiffStarRatingStandard[Mods.None]
            //    : -1;
            //DiffSrNoneTaiko = osuFile.DiffStarRatingTaiko.ContainsKey(Mods.None)
            //    ? osuFile.DiffStarRatingTaiko[Mods.None]
            //    : -1;
            //DiffSrNoneCtB = osuFile.DiffStarRatingCtB.ContainsKey(Mods.None) ? osuFile.DiffStarRatingCtB[Mods.None] : -1;
            //DiffSrNoneMania = osuFile.DiffStarRatingMania.ContainsKey(Mods.None)
            //    ? osuFile.DiffStarRatingMania[Mods.None]
            //    : -1;
            beatmap.DrainTimeSeconds = (int)(osuFile.HitObjects.MaxTime -
                                             osuFile.HitObjects.MinTime -
                                             osuFile.Events.Breaks.Select(k => k.EndTime - k.StartTime).Sum());
            beatmap.TotalTime = (int)osuFile.HitObjects.MaxTime;
            beatmap.AudioPreviewTime = osuFile.General.PreviewTime;
            beatmap.BeatmapId = osuFile.Metadata.BeatmapId;
            beatmap.BeatmapSetId = osuFile.Metadata.BeatmapSetId;
            beatmap.GameMode = osuFile.General.Mode;
            beatmap.SongSource = osuFile.Metadata.Source;
            beatmap.SongTags = string.Join(" ", osuFile.Metadata.TagList);
            //FolderName = osuFile.FolderName;
            beatmap.AudioFileName = osuFile.General.AudioFilename;

            beatmap.Id = Zip($"!{beatmap.FolderNameOrPath}|{beatmap.Version}|{(beatmap.InOwnDb ? 1 : 0)}");

            return beatmap;
        }

        public static Beatmap ParseFromOSharp(OsuFile osuFile)
        {
            return (new Beatmap()).UpdateFromOSharp(osuFile);
        }

        public static OSharpGameMode ParseHollyToOSharp(this osu.Shared.GameMode gameMode)
        {
            return (OSharpGameMode)(int)gameMode;
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }

            var array = mso.ToArray();
            return array;
        }

        public static string Unzip(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }

            var unzip = Encoding.UTF8.GetString(mso.ToArray());
            return unzip;
        }

        public static Beatmap ParseFromNost(MusicScore musicscore, string path)
        {
            return new Beatmap()
            {
                Artist = path,
                Title = path
                //Id = Zip($"{new Random().Next()}")
            };
        }
    }
}