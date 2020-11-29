using Milky.OsuPlayer.Data.Models;
using System.IO;

namespace Milky.OsuPlayer.Common
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
    }
}