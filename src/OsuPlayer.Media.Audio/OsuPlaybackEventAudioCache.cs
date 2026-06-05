using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KeyAsio.Core.Audio.Caching;
using KeyAsio.Core.OsuAudio.Hitsounds.Playback;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace OsuPlayer.Media.Audio;

public sealed class OsuPlaybackEventAudioCache
{
    private const string BeatmapCategory = "beatmap";
    private const string SkinCategory = "skin";

    private readonly AudioCacheManager _audioCacheManager;
    private readonly ILogger? _logger;
    private readonly Lock _gate = new();
    private readonly Dictionary<PlaybackEvent, CachedAudio?> _eventCache = new();
    private readonly Dictionary<string, CachedAudio?> _resourceCache = new(StringComparer.OrdinalIgnoreCase);

    private string _beatmapFolder = "";
    private string _userSkinFolder = "";
    private string _defaultHitsoundFolder = "";
    private WaveFormat _waveFormat = null!;
    private int _contextVersion;

    public OsuPlaybackEventAudioCache(AudioCacheManager audioCacheManager, ILogger? logger = null)
    {
        _audioCacheManager = audioCacheManager;
        _logger = logger;
    }

    public void SetContext(string beatmapFolder, string userSkinFolder, string defaultHitsoundFolder,
        WaveFormat waveFormat)
    {
        lock (_gate)
        {
            _beatmapFolder = beatmapFolder;
            _userSkinFolder = userSkinFolder;
            _defaultHitsoundFolder = defaultHitsoundFolder;
            _waveFormat = waveFormat;
            _contextVersion++;
            _eventCache.Clear();
            _resourceCache.Clear();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _contextVersion++;
            _eventCache.Clear();
            _resourceCache.Clear();
        }
    }

    public async Task<CachedAudio?> GetOrCreateAsync(PlaybackEvent playbackEvent,
        CancellationToken cancellationToken = default)
    {
        CacheContext context;
        lock (_gate)
        {
            if (_eventCache.TryGetValue(playbackEvent, out var cachedAudio))
            {
                return cachedAudio;
            }

            context = CreateContextSnapshot();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await LoadAsync(playbackEvent, context, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            ThrowIfContextChanged(context);
            _eventCache[playbackEvent] = loaded;
        }

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

            _ = await GetOrCreateAsync(playbackEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<CachedAudio?> LoadAsync(
        PlaybackEvent playbackEvent,
        CacheContext context,
        CancellationToken cancellationToken)
    {
        if (playbackEvent is ControlEvent
            {
                ControlEventType: ControlEventType.LoopStop or ControlEventType.Volume or ControlEventType.Balance
            })
        {
            return Task.FromResult<CachedAudio?>(null);
        }

        if (string.IsNullOrWhiteSpace(playbackEvent.Filename))
        {
            return Task.FromResult<CachedAudio?>(null);
        }

        var (path, category) = ResolvePath(playbackEvent, context);
        if (path == null)
        {
            _logger?.LogWarning("Audio resource not found: {Filename}", playbackEvent.Filename);
            return Task.FromResult<CachedAudio?>(null);
        }

        lock (_gate)
        {
            ThrowIfContextChanged(context);
            if (_resourceCache.TryGetValue(path, out var cachedAudio))
            {
                return Task.FromResult(cachedAudio);
            }
        }

        return LoadAndRememberAsync(path, category, context, cancellationToken);
    }

    private async Task<CachedAudio?> LoadAndRememberAsync(
        string path,
        string category,
        CacheContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (cachedAudio, status) = await _audioCacheManager
            .GetOrCreateOrEmptyFromFileAsync(path, context.WaveFormat, category).ConfigureAwait(false);

        if (status == CacheGetStatus.Failed)
        {
            _logger?.LogWarning("Failed to cache osu audio resource: {Path}", path);
        }

        lock (_gate)
        {
            ThrowIfContextChanged(context);
            _resourceCache[path] = cachedAudio;
        }

        return cachedAudio;
    }

    private (string? Path, string Category) ResolvePath(PlaybackEvent playbackEvent, CacheContext context)
    {
        var filename = playbackEvent.Filename!;
        if (playbackEvent.ResourceOwner == ResourceOwner.Beatmap)
        {
            var beatmapPath = Path.Combine(context.BeatmapFolder, filename);
            if (File.Exists(beatmapPath))
            {
                return (beatmapPath, BeatmapCategory);
            }
        }

        var skinPath = ResolveFromFolder(context.UserSkinFolder, filename);
        if (skinPath != null)
        {
            return (skinPath, SkinCategory);
        }

        var defaultPath = ResolveFromFolder(context.DefaultHitsoundFolder, filename);
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

    private CacheContext CreateContextSnapshot()
    {
        return new CacheContext(
            _contextVersion,
            _beatmapFolder,
            _userSkinFolder,
            _defaultHitsoundFolder,
            _waveFormat);
    }

    private void ThrowIfContextChanged(CacheContext context)
    {
        if (context.Version != _contextVersion)
        {
            throw new OperationCanceledException("The osu audio cache context changed while loading a resource.");
        }
    }

    private readonly record struct CacheContext(
        int Version,
        string BeatmapFolder,
        string UserSkinFolder,
        string DefaultHitsoundFolder,
        WaveFormat WaveFormat);
}
