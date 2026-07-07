using System;
using System.IO;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Data.Models;
using OsuPlayer.Iidx.Abstractions;
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

        if (map.SourceGame == Shared.SourceGame.Iidx)
        {
            isFromDb = true;
            freePath = null;
            var musicDataPath = AppSettings.Default?.General.IidxMusicDataPath;
            if (string.IsNullOrWhiteSpace(musicDataPath))
            {
                return null;
            }

            try
            {
                var layout = IidxDataLayout.FromMusicDataPath(musicDataPath);
                return layout.GetSoundFolder(ExtractMusicId(map.FolderName));
            }
            catch
            {
                return null;
            }
        }

        isFromDb = true;
        freePath = null;
        var songPath = map.InOwnDb ? AppPaths.Current.CustomSongPath : AppPaths.Current.OsuSongPath;
        return songPath == null ? null : Path.Combine(songPath, map.FolderName);
    }

    private static int ExtractMusicId(string folderName)
    {
        // FolderName follows "iidx-{musicId:D5}" convention set by IidxBeatmapFactory.
        const string prefix = "iidx-";
        if (folderName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(folderName.AsSpan(prefix.Length), out var id))
            {
                return id;
            }
        }

        return 0;
    }
}
