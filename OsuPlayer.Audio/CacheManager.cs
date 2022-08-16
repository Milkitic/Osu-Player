using System.Collections.Concurrent;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

namespace Milki.OsuPlayer.Audio;

public class CacheManager
{
    private CacheManager()
    {
    }

    public static CacheManager Instance { get; } = new();

    private readonly ConcurrentDictionary<HitsoundNode, CachedSound?> _hitsoundNodeToCachedSoundMapping = new();
    private readonly ConcurrentDictionary<string, CachedSound?> _filenameToCachedSoundMapping = new();

    public void AddCachedSound(string path, CachedSound? cachedSound)
    {
        _filenameToCachedSoundMapping.TryAdd(path, cachedSound);
    }

    public void AddCachedSound(HitsoundNode hitsoundNode, CachedSound? cachedSound)
    {
        _hitsoundNodeToCachedSoundMapping.TryAdd(hitsoundNode, cachedSound);
    }

    public bool TryGetAudioByNode(HitsoundNode playableNode, out CachedSound? cachedSound)
    {
        if (!_hitsoundNodeToCachedSoundMapping.TryGetValue(playableNode, out cachedSound)) return false;
        return playableNode is not PlayableNode || cachedSound != null;
    }

    public bool TryGetAudioByPath(string path, out CachedSound? cachedSound)
    {
        return _filenameToCachedSoundMapping.TryGetValue(path, out cachedSound);
    }

    private void CleanAudioCaches()
    {
        CachedSoundFactory.ClearCacheSounds();
        _hitsoundNodeToCachedSoundMapping.Clear();
        _filenameToCachedSoundMapping.Clear();
    }
}