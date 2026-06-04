using System;
using System.IO;

namespace Milky.OsuPlayer.Media.Audio;

/// <summary>
/// Resolves file paths for beatmap resources (map files, audio, backgrounds, video).
/// </summary>
public static class BeatmapPathResolver
{
    /// <summary>
    /// Resolves the full path to a .osu beatmap file, handling both database-sourced
    /// and free-path beatmaps.
    /// </summary>
    public static string ResolveBeatmapPath(string folder, string beatmapFileName, bool isFromDb, string freePath)
    {
        if (!isFromDb)
        {
            if (string.IsNullOrWhiteSpace(freePath))
            {
                throw new InvalidDataException("Beatmap path is empty.");
            }

            return freePath;
        }

        return ResolveChildPath(folder, beatmapFileName);
    }

    /// <summary>
    /// Combines a base folder with a child path, throwing if either is empty.
    /// </summary>
    /// <exception cref="InvalidDataException">Base folder or child path is empty.</exception>
    public static string ResolveChildPath(string baseFolder, string childPath)
    {
        if (string.IsNullOrWhiteSpace(baseFolder))
        {
            throw new InvalidDataException("Beatmap base folder is empty.");
        }

        if (string.IsNullOrWhiteSpace(childPath))
        {
            throw new InvalidDataException("Beatmap referenced file path is empty.");
        }

        return Path.Combine(baseFolder, childPath);
    }

    /// <summary>
    /// Combines a base folder with a child path, returning <c>null</c> if either is empty
    /// or the resulting file does not exist.
    /// </summary>
    public static string? TryResolveChildPath(string baseFolder, string childPath)
    {
        return string.IsNullOrWhiteSpace(baseFolder) || string.IsNullOrWhiteSpace(childPath)
            ? null
            : Path.Combine(baseFolder, childPath);
    }

    /// <summary>
    /// Resolves the background image path for a beatmap, falling back to the
    /// default registration image if the beatmap has no background or the file is missing.
    /// </summary>
    public static string? ResolveBackgroundPath(string baseFolder, string? backgroundFilename, string defaultImagePath)
    {
        if (!string.IsNullOrWhiteSpace(backgroundFilename))
        {
            var bgPath = TryResolveChildPath(baseFolder, backgroundFilename);
            if (bgPath != null && File.Exists(bgPath))
            {
                return bgPath;
            }
        }

        return File.Exists(defaultImagePath) ? defaultImagePath : null;
    }

    /// <summary>
    /// Resolves the default image path under the application resource directory.
    /// </summary>
    public static string GetDefaultImagePath(string resourcePath)
    {
        return Path.Combine(resourcePath, "official", "registration.jpg");
    }
}
