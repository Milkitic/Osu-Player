using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Milky.OsuPlayer.Media.Audio;

public sealed class OsuPlaybackEventAudioCache
{
    private const string BeatmapCategory = "beatmap";
    private const string SkinCategory = "skin";

    private readonly AudioCacheManager _audioCacheManager;
    private readonly ILogger? _logger;
    private readonly Dictionary<PlaybackEvent, CachedAudio?> _eventCache = new();
    private readonly Dictionary<string, CachedAudio?> _resourceCache = new(StringComparer.OrdinalIgnoreCase);

    private string _beatmapFolder = "";
    private string _userSkinFolder = "";
    private string _defaultHitsoundFolder = "";
    private WaveFormat _waveFormat = null!;

    public OsuPlaybackEventAudioCache(AudioCacheManager audioCacheManager, ILogger? logger = null)
    {
        _audioCacheManager = audioCacheManager;
        _logger = logger;
    }

    public void SetContext(string beatmapFolder, string userSkinFolder, string defaultHitsoundFolder, WaveFormat waveFormat)
    {
        _beatmapFolder = beatmapFolder;
        _userSkinFolder = userSkinFolder;
        _defaultHitsoundFolder = defaultHitsoundFolder;
        _waveFormat = waveFormat;
        _eventCache.Clear();
        _resourceCache.Clear();
    }

    public async Task<CachedAudio?> GetOrCreateAsync(PlaybackEvent playbackEvent,
        CancellationToken cancellationToken = default)
    {
        if (_eventCache.TryGetValue(playbackEvent, out var cachedAudio))
        {
            return cachedAudio;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadAsync(playbackEvent, cancellationToken).ConfigureAwait(false);
        _eventCache[playbackEvent] = loaded;
        return loaded;
    }

    public async Task PrecacheRangeAsync(IEnumerable<PlaybackEvent> events, double startMilliseconds,
        double endMilliseconds, CancellationToken cancellationToken = default)
    {
        foreach (var playbackEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (playbackEvent.Offset < startMilliseconds || playbackEvent.Offset >= endMilliseconds)
            {
                continue;
            }

            if (_eventCache.ContainsKey(playbackEvent))
            {
                continue;
            }

            _ = await GetOrCreateAsync(playbackEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<CachedAudio?> LoadAsync(PlaybackEvent playbackEvent, CancellationToken cancellationToken)
    {
        if (playbackEvent is ControlEvent { ControlEventType: ControlEventType.LoopStop or ControlEventType.Volume or ControlEventType.Balance })
        {
            return Task.FromResult<CachedAudio?>(null);
        }

        if (string.IsNullOrWhiteSpace(playbackEvent.Filename))
        {
            return Task.FromResult<CachedAudio?>(null);
        }

        var (path, category) = ResolvePath(playbackEvent);
        if (path == null)
        {
            _logger?.LogWarning("Audio resource not found: {Filename}", playbackEvent.Filename);
            return Task.FromResult<CachedAudio?>(null);
        }

        if (_resourceCache.TryGetValue(path, out var cachedAudio))
        {
            return Task.FromResult(cachedAudio);
        }

        return LoadAndRememberAsync(path, category, cancellationToken);
    }

    private async Task<CachedAudio?> LoadAndRememberAsync(string path, string category, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (cachedAudio, status) =
            await _audioCacheManager.GetOrCreateOrEmptyFromFileAsync(path, _waveFormat, category).ConfigureAwait(false);

        if (status == CacheGetStatus.Failed)
        {
            _logger?.LogWarning("Failed to cache osu audio resource: {Path}", path);
        }

        _resourceCache[path] = cachedAudio;
        return cachedAudio;
    }

    private (string? Path, string Category) ResolvePath(PlaybackEvent playbackEvent)
    {
        var filename = playbackEvent.Filename!;
        if (playbackEvent.ResourceOwner == ResourceOwner.Beatmap)
        {
            var beatmapPath = Path.Combine(_beatmapFolder, filename);
            if (File.Exists(beatmapPath))
            {
                return (beatmapPath, BeatmapCategory);
            }
        }

        var skinPath = ResolveFromFolder(_userSkinFolder, filename);
        if (skinPath != null)
        {
            return (skinPath, SkinCategory);
        }

        var defaultPath = ResolveFromFolder(_defaultHitsoundFolder, filename);
        return (defaultPath, SkinCategory);
    }

    private static string? ResolveFromFolder(string folder, string filename)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        var directPath = Path.Combine(folder, filename);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        foreach (var extension in new[] { ".wav", ".mp3", ".ogg" })
        {
            var path = Path.Combine(folder, nameWithoutExtension + extension);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }
}
