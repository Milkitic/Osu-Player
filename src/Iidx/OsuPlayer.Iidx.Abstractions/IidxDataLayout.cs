using System;
using System.IO;

namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// Resolves on-disk resource paths inside an IIDX <c>data</c> directory from a
/// configured <c>music_data.bin</c> path. The <c>music_data.bin</c> file lives
/// under <c>data/info/&lt;version&gt;/music_data.bin</c>; this class derives the
/// <c>data</c> root once and resolves chart / sound / background paths from it.
/// </summary>
public sealed class IidxDataLayout
{
    private const string SoundFolderName = "sound";
    private const string GraphicFolderName = "graphic";
    private const string MovieThumbnailFolderName = "movie_thumbnail";
    private const string InfoFolderName = "info";

    public string DataRoot { get; }
    public string SoundRoot { get; }
    public string GraphicRoot { get; }
    public string MovieThumbnailRoot { get; }
    public string InfoRoot { get; }

    private IidxDataLayout(string dataRoot)
    {
        DataRoot = dataRoot;
        SoundRoot = Path.Combine(dataRoot, SoundFolderName);
        GraphicRoot = Path.Combine(dataRoot, GraphicFolderName);
        MovieThumbnailRoot = Path.Combine(GraphicRoot, MovieThumbnailFolderName);
        InfoRoot = Path.Combine(dataRoot, InfoFolderName);
    }

    /// <summary>
    /// Derives an <see cref="IidxDataLayout"/> from a <c>music_data.bin</c> file path.
    /// Expected layout: <c>data/info/&lt;version&gt;/music_data.bin</c>.
    /// </summary>
    public static IidxDataLayout FromMusicDataPath(string musicDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(musicDataPath);
        if (!Path.IsPathRooted(musicDataPath))
        {
            musicDataPath = Path.GetFullPath(musicDataPath);
        }

        // music_data.bin -> version dir -> info -> data
        var versionDir = Path.GetDirectoryName(musicDataPath);
        if (versionDir == null)
        {
            throw new DirectoryNotFoundException(
                $"Cannot resolve IIDX data root: '{musicDataPath}' has no parent directory.");
        }

        var infoDir = Path.GetDirectoryName(versionDir);
        if (infoDir == null || !Path.GetFileName(infoDir).Equals(InfoFolderName, StringComparison.OrdinalIgnoreCase))
        {
            throw new DirectoryNotFoundException(
                $"Cannot resolve IIDX data root: '{musicDataPath}' is not under a 'data/info/<version>/' layout.");
        }

        var dataRoot = Path.GetDirectoryName(infoDir);
        if (dataRoot == null)
        {
            throw new DirectoryNotFoundException(
                $"Cannot resolve IIDX data root: '{infoDir}' has no parent directory.");
        }

        return new IidxDataLayout(dataRoot);
    }

    /// <summary>Format an IIDX music id as the canonical 5-digit folder name.</summary>
    public static string FormatMusicId(int musicId) => musicId.ToString("D5");

    /// <summary>Resolves the per-song sound folder: <c>data/sound/{musicId:D5}/</c>.</summary>
    public string GetSoundFolder(int musicId) => Path.Combine(SoundRoot, FormatMusicId(musicId));

    /// <summary>Resolves <c>data/sound/{musicId:D5}/{musicId:D5}.1</c> (the chart file).</summary>
    public string GetChartPath(int musicId)
    {
        var folder = GetSoundFolder(musicId);
        return Path.Combine(folder, FormatMusicId(musicId) + ".1");
    }

    /// <summary>
    /// Resolves the preferred audio container. IIDX ships <c>.2dx</c> or <c>.s3p</c>;
    /// returns whichever exists, preferring <c>.2dx</c>.
    /// </summary>
    public string? GetAudioPath(int musicId)
    {
        var folder = GetSoundFolder(musicId);
        var twoDx = Path.Combine(folder, FormatMusicId(musicId) + ".2dx");
        if (File.Exists(twoDx)) return twoDx;

        var s3p = Path.Combine(folder, FormatMusicId(musicId) + ".s3p");
        if (File.Exists(s3p)) return s3p;

        return null;
    }

    /// <summary>Resolves the background thumbnail under <c>data/graphic/movie_thumbnail/</c>.</summary>
    public string? GetThumbnailPath(int musicId)
    {
        var path = Path.Combine(MovieThumbnailRoot, FormatMusicId(musicId) + "_thum.png");
        return File.Exists(path) ? path : null;
    }
}