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
        var songPath = map.InOwnDb ? AppPaths.Current.CustomSongPath : AppPaths.Current.OsuSongPath;
        return songPath == null ? null : Path.Combine(songPath, map.FolderName);
    }
}
