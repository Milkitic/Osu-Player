using System.IO;
using OsuPlayer.Data.Models;
using OsuPlayer.Shared;

namespace OsuPlayer.Core;

public static class IdentifiableExtension
{
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
