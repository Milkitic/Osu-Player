using System.Collections.Concurrent;
using System.IO;
using Milki.Extensions.MixPlayer;

namespace Milki.OsuPlayer.Audio;

public class FileCache
{
    private readonly ConcurrentDictionary<string, string> _pathCache = new();

    public string GetFileUntilFind(string sourceFolder, string fileNameWithoutExtension)
    {
        var combine = Path.Combine(sourceFolder, fileNameWithoutExtension);
        if (_pathCache.TryGetValue(combine, out var value))
        {
            return value;
        }

        string path = "";
        foreach (var extension in Information.SupportExtensions)
        {
            path = Path.Combine(sourceFolder, fileNameWithoutExtension + extension);

            if (File.Exists(path))
            {
                _pathCache.TryAdd(combine, path);
                return path;
            }
        }

        _pathCache.TryAdd(combine, path);
        return path;
    }
}