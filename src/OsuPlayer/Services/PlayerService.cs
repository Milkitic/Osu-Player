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

public class PlayItemLoadingContext
{
    private readonly CancellationTokenSource _cancellationTokenSource;

    public PlayItemLoadingContext(CancellationTokenSource cancellationTokenSource)
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
}

public class PlayerService : VmBase, IDisposable
{
    public event Func<PlayItemLoadingContext, ValueTask>? PreLoadStarted;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadStarted;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadMetaFinished;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadBackgroundInfoFinished;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadMusicFinished;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadVideoRequested;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadStoryboardRequested;
    public event Func<PlayItemLoadingContext, ValueTask>? LoadFinished;

    public event Func<OsuMixPlayer, ValueTask>? PlayerStarted;
    public event Func<OsuMixPlayer, ValueTask>? PlayerPaused;
    public event Func<OsuMixPlayer, ValueTask>? PlayerStopped;
    public event Func<OsuMixPlayer, ValueTask>? PlayerSeeked;

    private readonly PlayListService _playListService;
    private readonly AsyncLock _initializationLock = new();

    private AudioPlaybackEngine _audioPlaybackEngine;
    private bool _isInitializing;
    private string? _preStandardizedFolder;

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

    public async ValueTask InitializeNewAsync(string path, bool playInstant)
    {
        using var disposable = await _initializationLock.LockAsync().ConfigureAwait(false);
        IsInitializing = true;
        using var cts = new CancellationTokenSource();
        var context = new PlayItemLoadingContext(cts);

        try
        {
            await UninitializeCurrent().ConfigureAwait(false);


            LogTo.Info("Start load new song from path: {0}", path);
            PreLoadStarted?.Invoke(context);

            var osuFile = await OsuFile.ReadFromFileAsync(path, options =>
            {
                options.ExcludeSection("Editor");
                //options.IgnoreStoryboard();
            });

            context.OsuFile = osuFile;
            var standardizedPath = PathUtils.StandardizePath(path, AppSettings.Default.GeneralSection.OsuSongDir);

            await using var dbContext = App.Current.ServiceProvider.GetService<ApplicationDbContext>()!;
            var playItem = await dbContext.GetOrAddPlayItem(standardizedPath);

            var index = _playListService.SetPointerByPath(standardizedPath, true);
            context.PlayInstant = playInstant;
            context.PlayItem = playItem;

            LoadStarted?.Invoke(context);

            var folder = Path.GetDirectoryName(path)!;
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
            LoadMetaFinished?.Invoke(context);

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

            LoadBackgroundInfoFinished?.Invoke(context);

            // music
            context.MusicPath = Path.Combine(folder, osuFile.General?.AudioFilename ?? "audio.mp3");

            if (standardizedFolder != _preStandardizedFolder)
            {
                CachedSoundFactory.ClearCacheSounds();
                _preStandardizedFolder = standardizedFolder;
            }

            ActiveMixPlayer = new OsuMixPlayer(osuFile, _audioPlaybackEngine)
            {
                Offset = playItem.PlayItemConfig?.Offset ?? 0
            };

            ActiveMixPlayer.PlayStatusChanged += Player_PlayStatusChanged;
            ActiveMixPlayer.PositionUpdated += Player_PositionUpdated;
            await ActiveMixPlayer.InitializeAsync().ConfigureAwait(false);
            LoadMusicFinished?.Invoke(context);

            // video
            var videoName = osuFile.Events?.VideoInfo?.Filename;

            if (videoName != null)
            {
                var videoPath = Path.Combine(folder, videoName);
                if (File.Exists(videoPath))
                {
                    context.VideoPath = videoPath;
                    LoadVideoRequested?.Invoke(context);
                }
            }

            // storyboard
            if (!string.IsNullOrWhiteSpace(osuFile.Events?.StoryboardText))
            {
                LoadStoryboardRequested?.Invoke(context);
            }
            else if (await osuFile.OsuFileHasOsbStoryboard().ConfigureAwait(false))
            {
                LoadStoryboardRequested?.Invoke(context);
            }

            context.IsLoaded = true;
            LoadFinished?.Invoke(context);

            await dbContext.SaveChangesAsync(cts.Token);
            await dbContext.AddOrUpdateBeatmapToRecentPlayAsync(playItem, DateTime.Now);
        }
        catch (Exception ex)
        {
            var errorMessage = context.PlayItem?.Path == null
                ? $"Error while loading new beatmap."
                : $"Error while loading new beatmap. Beatmap file: {context.PlayItem?.Path}";
            LogTo.ErrorException(errorMessage, ex);

            if (ActiveMixPlayer?.PlayerStatus != PlayerStatus.Playing)
            {
                await PlayByControl(PlayDirection.Next, false).ConfigureAwait(false);
            }
        }
        finally
        {
            IsInitializing = false;
        }
    }

    public void Dispose()
    {
        _initializationLock.Dispose();
    }

    private async Task PlayByControl(PlayDirection direction, bool auto)
    {
        try
        {
            if (!auto)
            {
                InterruptPrevOperation();
            }

            var currentPath = _playListService.GetCurrentPath();
            var controlResult = auto
                    ? await PlayList.InvokeAutoNext().ConfigureAwait(false)
                    : await PlayList.SwitchByControl(direction).ConfigureAwait(false);
            if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Default &&
                controlResult.PlayStatus == PlayControlResult.PlayControlStatus.Play)
            {
                if (_playListService.GetCurrentPath() == null)
                {
                    await ClearPlayer().ConfigureAwait(false);
                    Execute.OnUiThread(() => InterfaceClearRequest?.Invoke());
                    return;
                }

                if (currentPath == PlayList.CurrentInfo)
                {
                    await PlayList.CurrentInfo.StopHandle().ConfigureAwait(false);
                    await PlayList.CurrentInfo.PlayHandle().ConfigureAwait(false);
                    return;
                }

                InitializeContextHandle(PlayList.CurrentInfo);
                if (await LoadAsync(false, true).ConfigureAwait(false))
                {
                    await PlayList.CurrentInfo.PlayHandle.Invoke().ConfigureAwait(false);
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Keep)
            {
                switch (controlResult.PlayStatus)
                {
                    case PlayControlResult.PlayControlStatus.Play:
                        await PlayList.CurrentInfo.RestartHandle.Invoke().ConfigureAwait(false);
                        break;
                    case PlayControlResult.PlayControlStatus.Stop:
                        await PlayList.CurrentInfo.StopHandle.Invoke().ConfigureAwait(false);
                        break;
                }
            }
            else if (controlResult.PointerStatus == PlayControlResult.PointerControlStatus.Clear)
            {
                await ClearPlayer().ConfigureAwait(false);
                Execute.OnUiThread(() => InterfaceClearRequest?.Invoke());
                return;
            }
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while changing song.", ex);
        }
    }


    private static string? GetDefaultPath()
    {
        var defaultDir = Path.Combine(AppSettings.Directories.OfficialBgDir);
        var files = new DirectoryInfo(defaultDir).GetFiles("*.jpg");
        if (files.Length == 0) return null;
        var defaultPath = files[Random.Shared.Next(files.Length)].FullName;
        return defaultPath;
    }


    private async Task UninitializeCurrent()
    {
        if (ActiveMixPlayer == null) return;
        if (PlayerStopped != null)
        {
            await PlayerStopped.Invoke(ActiveMixPlayer).ConfigureAwait(false);
        }

        ActiveMixPlayer.PlayStatusChanged -= Player_PlayStatusChanged;
        ActiveMixPlayer.PositionUpdated -= Player_PositionUpdated;
        await ActiveMixPlayer.DisposeAsync().ConfigureAwait(false);
        ActiveMixPlayer = null;
    }
}