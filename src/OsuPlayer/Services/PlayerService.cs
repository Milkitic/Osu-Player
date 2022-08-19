#nullable enable

using System.IO;
using Anotar.NLog;
using Coosu.Beatmap;
using Coosu.Beatmap.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.NAudioExtensions.Wave;
using Milki.OsuPlayer.Audio;
using Milki.OsuPlayer.Audio.Mixing;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.Services;

[Fody.ConfigureAwait(false)]
public class PlayerService : VmBase, IAsyncDisposable
{
    public class PlayItemLoadContext
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public PlayItemLoadContext(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        public LocalOsuFile? OsuFile { get; set; }
        public bool PlayInstant { get; set; }

        public PlayItem? PlayItem { get; set; }
        public bool IsPlayItemFavorite { get; set; }

        public string? BackgroundPath { get; set; }
        public string? MusicPath { get; set; }
        public string? VideoPath { get; set; }

        public bool IsLoaded { get; set; }
        public OsuMixPlayer? Player { get; set; }
    }

    public event Func<PlayItemLoadContext, ValueTask>? LoadPreStarted;
    public event Func<PlayItemLoadContext, ValueTask>? LoadStarted;
    public event Func<PlayItemLoadContext, ValueTask>? LoadMetaFinished;
    public event Func<PlayItemLoadContext, ValueTask>? LoadBackgroundInfoFinished;
    public event Func<PlayItemLoadContext, ValueTask>? LoadMusicFinished;
    public event Func<PlayItemLoadContext, ValueTask>? LoadVideoRequested;
    public event Func<PlayItemLoadContext, ValueTask>? LoadStoryboardRequested;
    public event Func<PlayItemLoadContext, ValueTask>? LoadFinished;

    public event Func<OsuMixPlayer, ValueTask>? PlayerStarted;
    public event Func<OsuMixPlayer, ValueTask>? PlayerPaused;
    public event Func<OsuMixPlayer, ValueTask>? PlayerStopped;
    public event Func<OsuMixPlayer, TimeSpan, ValueTask>? PlayerSeek;

    public event Action<PlayerStatus>? PlayerStatusChanged;
    public event Action<TimeSpan>? PlayTimeChanged;

    private readonly PlayListService _playListService;
    private readonly AsyncLock _initializationLock = new();

    private AudioPlaybackEngine _audioPlaybackEngine;
    private bool _isInitializing;
    private string? _lastStandardizedFolder;
    private CancellationTokenSource? _lastInitCts;

    private TimeSpan _playTime;
    private TimeSpan _totalTime;

    public PlayerService(PlayListService playListService, AppSettings appSettings)
    {
        _playListService = playListService;
        _audioPlaybackEngine =
            new AudioPlaybackEngine(appSettings.PlaySection.DeviceInfo, 48000, notifyProgress: false);
    }

    public bool IsInitializing
    {
        get => _isInitializing;
        set => this.RaiseAndSetIfChanged(ref _isInitializing, value);
    }

    public OsuMixPlayer? ActiveMixPlayer { get; private set; }

    public PlayItemLoadContext? LastLoadContext { get; private set; }

    public TimeSpan PlayTime
    {
        get => _playTime;
        set => this.RaiseAndSetIfChanged(ref _playTime, value);
    }

    public TimeSpan TotalTime
    {
        get => _totalTime;
        set => this.RaiseAndSetIfChanged(ref _totalTime, value);
    }

    public PlayerStatus PlayerStatus => ActiveMixPlayer?.PlayerStatus ?? PlayerStatus.Uninitialized;

    public async ValueTask PlayAsync()
    {
        var activeMixPlayer = ActiveMixPlayer;
        if (activeMixPlayer == null) return;
        if (activeMixPlayer.PlayerStatus == PlayerStatus.Playing) return;
        await activeMixPlayer.Play();
        if (PlayerStarted != null) await PlayerStarted.Invoke(activeMixPlayer);
    }

    public async ValueTask StopAsync()
    {
        var activeMixPlayer = ActiveMixPlayer;
        if (activeMixPlayer == null) return;
        if (activeMixPlayer.PlayerStatus == PlayerStatus.Ready) return;
        await activeMixPlayer.Stop();
        if (PlayerStopped != null) await PlayerStopped.Invoke(activeMixPlayer);
    }

    public async ValueTask TogglePlayAsync()
    {
        if (PlayerStatus == PlayerStatus.Playing)
        {
            await PauseAsync();
        }
        else if (PlayerStatus is PlayerStatus.Paused or PlayerStatus.Ready)
        {
            await PlayAsync();
        }
    }

    public async ValueTask PauseAsync()
    {
        var activeMixPlayer = ActiveMixPlayer;
        if (activeMixPlayer == null) return;
        if (activeMixPlayer.PlayerStatus == PlayerStatus.Paused) return;
        await activeMixPlayer.Pause();
        if (PlayerPaused != null) await PlayerPaused.Invoke(activeMixPlayer);
    }

    public async ValueTask SeekAsync(TimeSpan time)
    {
        var playAfterSeek = PlayerStatus == PlayerStatus.Playing;
        var activeMixPlayer = ActiveMixPlayer;
        if (activeMixPlayer == null) return;
        await activeMixPlayer.Seek(time);
        if (PlayerSeek != null) await PlayerSeek.Invoke(activeMixPlayer, time);
    }

    public async ValueTask PlayPreviousAsync()
    {
        await PlayByControl(PlayDirection.Previous, true);
    }

    public async ValueTask PlayNextAsync()
    {
        await PlayByControl(PlayDirection.Next, true);
    }

    public async ValueTask InitializeNewAsync(string standardizedPath, bool playInstant)
    {
        CancelPreviousInitialization();

        using var disposable = await _initializationLock.LockAsync();
        IsInitializing = true;
        _lastInitCts ??= new CancellationTokenSource();
        var context = new PlayItemLoadContext(_lastInitCts);

        try
        {
            await DisposeActiveMixPlayer();

            LogTo.Info("Start load new song from path: {0}", standardizedPath);
            if (LoadPreStarted != null) await LoadPreStarted.Invoke(context);
            var path = PathUtils.GetFullPath(standardizedPath, AppSettings.Default.GeneralSection.OsuSongDir);
            var osuFile = await OsuFile.ReadFromFileAsync(path, options =>
            {
                options.ExcludeSection("Editor");
                //options.IgnoreStoryboard();
            });

            context.OsuFile = osuFile;
            await using var dbContext = App.Current.ServiceProvider.GetService<ApplicationDbContext>()!;
            var playItem = await dbContext.GetOrAddPlayItem(standardizedPath);

            var index = _playListService.SetPointerByPath(standardizedPath, true);
            context.PlayInstant = playInstant;
            context.PlayItem = playItem;

            if (LoadStarted != null) await LoadStarted.Invoke(context);

            var folder = Path.GetDirectoryName(standardizedPath)!;
            var standardizedFolder = PathUtils.GetFolder(standardizedPath);

            bool isFavorite = playItem.PlayLists.Any(k => k.IsDefault);
            context.IsPlayItemFavorite = isFavorite;

            playItem.PlayItemDetail.Artist = osuFile.Metadata?.Artist ?? "";
            playItem.PlayItemDetail.ArtistUnicode = osuFile.Metadata?.ArtistUnicode ?? "";
            playItem.PlayItemDetail.Title = osuFile.Metadata?.Title ?? "";
            playItem.PlayItemDetail.TitleUnicode = osuFile.Metadata?.TitleUnicode ?? "";
            playItem.PlayItemDetail.BeatmapId = osuFile.Metadata?.BeatmapId ?? -1;
            playItem.PlayItemDetail.BeatmapSetId = osuFile.Metadata?.BeatmapSetId ?? -1;
            playItem.PlayItemDetail.Creator = osuFile.Metadata?.Creator ?? "";
            playItem.PlayItemDetail.Version = osuFile.Metadata?.Version ?? "";
            playItem.PlayItemDetail.Source = osuFile.Metadata?.Source ?? "";
            var tagList = osuFile.Metadata?.TagList ?? (IReadOnlyList<string>)Array.Empty<string>();
            playItem.PlayItemDetail.Tags = string.Join(' ', tagList);
            if (LoadMetaFinished != null) await LoadMetaFinished.Invoke(context);

            if (osuFile.Events?.BackgroundInfo != null)
            {
                var bgPath = Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename);
                if (File.Exists(bgPath))
                {
                    context.BackgroundPath = bgPath;
                }
                else
                {
                    var defaultPath = GetDefaultPath();
                    context.BackgroundPath = defaultPath;
                }
            }
            else
            {
                var defaultPath = GetDefaultPath();
                context.BackgroundPath = defaultPath;
            }

            if (LoadBackgroundInfoFinished != null) await LoadBackgroundInfoFinished.Invoke(context);

            // music
            context.MusicPath = Path.Combine(folder, osuFile.General?.AudioFilename ?? "audio.mp3");

            if (standardizedFolder != _lastStandardizedFolder)
            {
                CachedSoundFactory.ClearCacheSounds();
                _lastStandardizedFolder = standardizedFolder;
            }

            ActiveMixPlayer = new OsuMixPlayer(osuFile, _audioPlaybackEngine)
            {
                Offset = playItem.PlayItemConfig?.Offset ?? 0
            };

            ActiveMixPlayer.PlayerStatusChanged += Player_PlayerStatusChanged;
            ActiveMixPlayer.PositionChanged += Player_PositionChanged;
            await ActiveMixPlayer.InitializeAsync();
            Execute.OnUiThread(() => TotalTime = ActiveMixPlayer.TotalTime);
            context.Player = ActiveMixPlayer;
            if (LoadMusicFinished != null) await LoadMusicFinished.Invoke(context);

            // video
            var videoName = osuFile.Events?.VideoInfo?.Filename;

            if (videoName != null)
            {
                var videoPath = Path.Combine(folder, videoName);
                if (File.Exists(videoPath))
                {
                    context.VideoPath = videoPath;
                    if (LoadVideoRequested != null) await LoadVideoRequested.Invoke(context);
                }
            }

            // storyboard
            if (!string.IsNullOrWhiteSpace(osuFile.Events?.StoryboardText) ||
                await osuFile.OsuFileHasOsbStoryboard())
            {
                if (LoadStoryboardRequested != null) await LoadStoryboardRequested.Invoke(context);
            }

            context.IsLoaded = true;
            LastLoadContext = context;
            if (LoadFinished != null) await LoadFinished.Invoke(context);

            await dbContext.SaveChangesAsync(_lastInitCts.Token);
            await dbContext.AddOrUpdatePlayItemToRecentPlayAsync(playItem, DateTime.Now);
        }
        catch (Exception ex)
        {
            var errorMessage = context.PlayItem?.StandardizedPath == null
                ? $"Error while loading new beatmap."
                : $"Error while loading new beatmap. Beatmap file: {context.PlayItem?.StandardizedPath}";
            LogTo.ErrorException(errorMessage, ex);

            if (ActiveMixPlayer?.PlayerStatus != PlayerStatus.Playing)
            {
                await PlayByControl(PlayDirection.Next, false);
            }
        }
        finally
        {
            IsInitializing = false;
        }
    }

    private async void Player_PlayerStatusChanged(TrackPlayer trackPlayer, PlayerStatus oldStatus, PlayerStatus newStatus)
    {
        PlayerStatusChanged?.Invoke(newStatus);
        if (newStatus == PlayerStatus.Ready && trackPlayer.Position.Equals(trackPlayer.Duration))
        {
            await PlayByControl(PlayDirection.Next, false);
        }
    }

    private void Player_PositionChanged(TrackPlayer trackPlayer, double oldPosition, double newPosition)
    {
        Execute.OnUiThread(() => PlayTime = TimeSpan.FromMilliseconds(newPosition));
        PlayTimeChanged?.Invoke(PlayTime);
    }

    private async ValueTask PlayByControl(PlayDirection direction, bool isManual)
    {
        try
        {
            var currentPath = _playListService.GetCurrentPath();
            var nextPath = _playListService.GetAndSetNextPath(direction, isManual);
            if (nextPath == null)
            {
                await StopAsync();
            }
            else if (nextPath == currentPath)
            {
                await StopAsync();
                await PlayAsync();
            }
            else if (nextPath != currentPath)
            {
                await InitializeNewAsync(nextPath, true);
            }
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while changing song.", ex);
        }
    }

    private void CancelPreviousInitialization()
    {
        if (_lastInitCts != null)
        {
            _lastInitCts.Cancel();
            _lastInitCts.Dispose();
        }

        _lastInitCts = new CancellationTokenSource();
    }

    private static string? GetDefaultPath()
    {
        var defaultDir = Path.Combine(AppSettings.Directories.OfficialBgDir);
        var files = new DirectoryInfo(defaultDir).GetFiles("*.jpg");
        if (files.Length == 0) return null;
        var defaultPath = files[Random.Shared.Next(files.Length)].FullName;
        return defaultPath;
    }

    private async ValueTask DisposeActiveMixPlayer()
    {
        if (ActiveMixPlayer == null) return;
        if (PlayerStopped != null) await PlayerStopped.Invoke(ActiveMixPlayer);

        ActiveMixPlayer.PlayerStatusChanged -= Player_PlayerStatusChanged;
        ActiveMixPlayer.PositionChanged -= Player_PositionChanged;
        await ActiveMixPlayer.DisposeAsync();
        ActiveMixPlayer = null;
    }

    public async ValueTask DisposeAsync()
    {
        CancelPreviousInitialization();
        await DisposeActiveMixPlayer();
        _initializationLock.Dispose();
        _lastInitCts?.Dispose();
    }
}