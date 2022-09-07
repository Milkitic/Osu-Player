using System.Collections.Concurrent;
using Coosu.Beatmap.Extensions.Playback;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;

namespace Milki.OsuPlayer.Audio;

public class AudioCacheManager
{
    private readonly ConcurrentDictionary<string, CachedSound?> _filenameToCachedSoundMapping = new();

    private readonly ConcurrentDictionary<HitsoundNode, CachedSound?> _hitsoundNodeToCachedSoundMapping = new();

    private AudioCacheManager()
    {
    }

    public static AudioCacheManager Instance { get; } = new();

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