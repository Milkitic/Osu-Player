using System.Collections.Concurrent;
using System.IO;

namespace Milky.OsuPlayer.Media.Audio
{
    public class HitsoundFileCache
    {
        private static readonly string[] SupportExtensions = { ".wav", ".mp3", ".ogg" };

        private readonly ConcurrentDictionary<string, string> _pathCache = new ConcurrentDictionary<string, string>();

        public string GetFileUntilFind(string sourceFolder, string fileNameWithoutExtension, out bool found)
        {
            var cacheKey = Path.Combine(sourceFolder ?? string.Empty, fileNameWithoutExtension ?? string.Empty);
            if (_pathCache.TryGetValue(cacheKey, out var value))
            {
                found = File.Exists(value);
                return value;
            }

            foreach (var extension in SupportExtensions)
            {
                var path = Path.Combine(sourceFolder ?? string.Empty, (fileNameWithoutExtension ?? string.Empty) + extension);
                if (!File.Exists(path))
                {
                    continue;
                }

                _pathCache.TryAdd(cacheKey, path);
                found = true;
                return path;
            }

            _pathCache.TryAdd(cacheKey, string.Empty);
            found = false;
            return string.Empty;
        }
    }
}