#nullable enable

using System.IO;
using System.Windows.Media.Imaging;
using Anotar.NLog;
using Coosu.Beatmap;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Utils;

namespace Milki.OsuPlayer;

[Fody.ConfigureAwait(false)]
public static class CommonUtils
{
    private static readonly SemaphoreSlim ConcurrentLimit =
        new(Environment.ProcessorCount == 1 ? Environment.ProcessorCount : Environment.ProcessorCount - 1);

    public static bool? BrowseDb(out string path)
    {
        var fbd = new OpenFileDialog
        {
            Title = @"请选择osu所在目录内的""osu!.db""",
            Filter = @"Beatmap Database|osu!.db"
        };
        var result = fbd.ShowDialog();
        path = fbd.FileName;
        return result;
    }

    public static async Task SyncOsuDbAsync(this BeatmapSyncService syncService, string path)
    {
        try
        {
            await syncService.SynchronizeManaged(syncService.GetPlayItemDetailsFormDb(path));

            AppSettings.Default.GeneralSection.DbPath = path;
            AppSettings.SaveDefault();

            await using var dbContext = ServiceProviders.GetApplicationDbContext();
            var softwareState = await dbContext.GetSoftwareState();

            softwareState.LastSync = DateTime.Now;
            await dbContext.UpdateAndSaveChangesAsync(softwareState, k => k.LastSync);
        }
        catch (Exception ex)
        {
            LogTo.ErrorException($"Error while syncing osu!db: {path}", ex);
            throw;
        }
    }

    public static async Task SyncCustomFolderAsync(this OsuFileScanningService osuFileScanningService, string path)
    {
        try
        {
            await osuFileScanningService.CancelTaskAsync();
            await osuFileScanningService.ScanAndSyncAsync(path);

            AppSettings.Default.GeneralSection.CustomSongDir = path;
            AppSettings.SaveDefault();
        }
        catch (Exception ex)
        {
            LogTo.ErrorException($"Error while scanning custom folder: {path}", ex);
            throw;
        }
    }

    public static async Task<string?> GetThumbByBeatmapDbId(PlayItem playItem)
    {
        if (playItem.PlayItemAsset?.ThumbPath != null)
        {
            var fullPath = Path.Combine(AppSettings.Directories.ThumbCacheDir, playItem.PlayItemAsset.ThumbPath);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        var osuFilePath = PathUtils.GetFullPath(playItem.StandardizedPath, AppSettings.Default.GeneralSection.OsuSongDir);

        if (!File.Exists(osuFilePath))
        {
            return null;
        }

        await ConcurrentLimit.WaitAsync();
        try
        {
            LocalOsuFile osuFile;
            try
            {
                osuFile = await OsuFile.ReadFromFileAsync(osuFilePath, options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                });
            }
            catch (Exception ex)
            {
                LogTo.WarnException(
                    $"Error while creating beatmap thumb cache caused by reading osu file: {playItem.StandardizedPath}", ex);
                return null;
            }

            var guidStr = Guid.NewGuid().ToString("N");

            var bgFilename = osuFile.Events?.BackgroundInfo?.Filename;
            if (string.IsNullOrWhiteSpace(bgFilename))
            {
                return null;
            }

            var folder = Path.GetDirectoryName(osuFilePath)!;
            var sourceBgPath = Path.Combine(folder, bgFilename);

            if (!File.Exists(sourceBgPath))
            {
                return null;
            }

            ResizeImageAndSave(sourceBgPath, guidStr, height: 200);
            await using var dbContext = ServiceProviders.GetApplicationDbContext();
            var asset = await dbContext.PlayItemAssets.FindAsync(playItem.PlayItemAsset?.Id);
            if (asset == null)
            {
                playItem.PlayItemAsset = asset = new PlayItemAsset();
                dbContext.PlayItemAssets.Add(asset);
                await dbContext.SaveChangesAsync();
                playItem.PlayItemAssetId = asset.Id;
                dbContext.Update(playItem, k => k.PlayItemAssetId);
            }

            asset.ThumbPath = $"{guidStr}.jpg";
            await dbContext.SaveChangesAsync();
            return Path.Combine(AppSettings.Directories.ThumbCacheDir, asset.ThumbPath);
        }
        catch (Exception ex)
        {
            LogTo.WarnException(
                $"Error while creating beatmap thumb cache: {playItem.StandardizedPath}", ex);
            return null;
        }
        finally
        {
            ConcurrentLimit.Release();
        }
    }

    public static async Task<bool> AddToCollectionAsync(PlayList playList, IList<PlayItem> beatmaps)
    {
        if (beatmaps.Count <= 0) return false;

        await using var dbContext = ServiceProviders.GetApplicationDbContext();
        if (string.IsNullOrEmpty(playList.ImagePath))
        {
            var osuSongDir = AppSettings.Default.GeneralSection.OsuSongDir;
            foreach (var beatmap in beatmaps)
            {
                var folder = PathUtils.GetFullPath(beatmap.StandardizedFolder, osuSongDir);
                var path = PathUtils.GetFullPath(beatmap.StandardizedPath, osuSongDir);
                try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(path, options =>
                    {
                        options.IncludeSection("Events");
                        options.IgnoreSample();
                        options.IgnoreStoryboard();
                    });
                    if (osuFile.Events?.BackgroundInfo == null)
                        continue;

                    var imagePath = Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename);
                    if (!File.Exists(imagePath)) continue;

                    playList.ImagePath = imagePath;
                    await dbContext.UpdateAndSaveChangesAsync(playList, k => k.ImagePath);
                    break;
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }

        await dbContext.AddPlayItemsToPlayList(beatmaps, playList);
        if (playList.IsDefault)
        {
            var playerService = App.Current.ServiceProvider.GetService<PlayerService>()!;
            var loadContext = playerService.LastLoadContext;
            if (loadContext?.PlayItem != null && beatmaps.Any(k => loadContext.PlayItem.Id.Equals(k.Id)))
            {
                loadContext.IsPlayItemFavorite = true;
            }
        }

        return true;
    }

    private static void ResizeImageAndSave(string sourcePath, string targetName, int width = 0, int height = 0)
    {
        var imageBytes = LoadImageData(sourcePath);
        var bitmapSource = CreateImage(imageBytes, width, height, out var memoryStream);
        using (memoryStream)
        {
            imageBytes = GetEncodedImageData(bitmapSource, ".jpg");
            var filePath = Path.Combine(AppSettings.Directories.ThumbCacheDir, $"{targetName}.jpg");
            SaveImageData(imageBytes, filePath);
        }
    }

    private static byte[] LoadImageData(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }

    private static BitmapSource CreateImage(byte[] imageData, int decodePixelWidth, int decodePixelHeight, out MemoryStream memoryStream)
    {
        var result = new BitmapImage();
        result.BeginInit();
        if (decodePixelWidth > 0)
        {
            result.DecodePixelWidth = decodePixelWidth;
        }

        if (decodePixelHeight > 0)
        {
            result.DecodePixelHeight = decodePixelHeight;
        }

        memoryStream = new MemoryStream(imageData);
        result.StreamSource = memoryStream;
        result.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        result.CacheOption = BitmapCacheOption.None;
        result.EndInit();
        return result;
    }

    private static void SaveImageData(byte[] imageData, string filePath)
    {
        File.WriteAllBytes(filePath, imageData);
    }

    private static byte[] GetEncodedImageData(BitmapSource source, string preferredFormat)
    {
        BitmapEncoder encoder;
        switch (preferredFormat.ToLower())
        {
            case ".jpg":
            case ".jpeg":
                encoder = new JpegBitmapEncoder();
                break;
            case ".bmp":
                encoder = new BmpBitmapEncoder();
                break;
            case ".png":
                encoder = new PngBitmapEncoder();
                break;
            case ".tif":
            case ".tiff":
                encoder = new TiffBitmapEncoder();
                break;
            case ".gif":
                encoder = new GifBitmapEncoder();
                break;
            case ".wmp":
                encoder = new WmpBitmapEncoder();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}