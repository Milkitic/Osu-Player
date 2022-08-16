using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coosu.Beatmap.Sections.GamePlay;
using Milki.OsuPlayer.Data.Models;

namespace Milki.OsuPlayer.Common
{
    public static class BeatmapExtension
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static string GetFolder(this Beatmap map, out bool isFromDb, out string freePath)
        {
            if (map.IsTemporary)
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

        public static Beatmap GetHighestDiff(this IEnumerable<Beatmap> enumerable)
        {
            var random = new Random(DateTime.Now.Ticks.GetHashCode());
            var dictionary = enumerable.GroupBy(k => k.GameMode).ToDictionary(k => k.Key, k => k.ToList());
            if (dictionary.ContainsKey(GameMode.Circle))
            {
                return dictionary[GameMode.Circle]
                    .Aggregate((i1, i2) => i1.DiffSrNoneStandard > i2.DiffSrNoneStandard ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Mania))
            {
                return dictionary[GameMode.Mania]
                    .Aggregate((i1, i2) => i1.DiffSrNoneMania > i2.DiffSrNoneMania ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Catch))
            {
                return dictionary[GameMode.Catch]
                    .Aggregate((i1, i2) => i1.DiffSrNoneCtB > i2.DiffSrNoneCtB ? i1 : i2);
            }

            if (dictionary.ContainsKey(GameMode.Taiko))
            {
                return dictionary[GameMode.Taiko]
                    .Aggregate((i1, i2) => i1.DiffSrNoneTaiko > i2.DiffSrNoneTaiko ? i1 : i2);
            }

            Logger.Warn(@"Get highest difficulty failed.");
            var randKey = dictionary.Keys.ToList()[random.Next(dictionary.Keys.Count)];
            return dictionary[randKey][dictionary[randKey].Count];
        }
    }
}