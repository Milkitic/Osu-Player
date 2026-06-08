using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Coosu.Beatmap;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Shared;

namespace OsuPlayer.Services;

public sealed class WpfBeatmapThumbnailService : IBeatmapThumbnailService
{
    private readonly ILogger<WpfBeatmapThumbnailService> _logger;
    private readonly IPlayerDataStore _playerData;
    private readonly SemaphoreSlim _lock = new(5);

    public WpfBeatmapThumbnailService(IPlayerDataStore playerData, ILogger<WpfBeatmapThumbnailService> logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    public async Task<string> GetThumbByBeatmapDbIdAsync(BeatmapDataModel dataModel)
    {
        return await Task.Run(async () =>
        {
            await _lock.WaitAsync();
            try
            {
                var (found, path) = await _playerData.TryGetMapThumbAsync(dataModel.BeatmapDbId);
                if (found && path != null)
                {
                    if (File.Exists(path)) return path;
                }

                var folder = dataModel.GetFolder(out var isFromDb, out var freePath);
                var osuFilePath = isFromDb ? Path.Combine(folder, dataModel.BeatmapFileName) : freePath;

                if (!File.Exists(osuFilePath))
                {
                    return null;
                }

                var osuFile = await OsuFile.ReadFromFileAsync(osuFilePath, options =>
                    {
                        options.IncludeSection("Events");
                        options.IgnoreSample();
                        options.IgnoreStoryboard();
                    })
                    .ConfigureAwait(false);

                var guidStr = Guid.NewGuid().ToString();

                var sourceBgFile = osuFile.Events?.BackgroundInfo?.Filename;
                if (string.IsNullOrWhiteSpace(sourceBgFile))
                {
                    await _playerData.TrySetMapThumbAsync(dataModel.BeatmapDbId, null);
                    return null;
                }

                var sourceBgPath = Path.Combine(folder, sourceBgFile);

                if (!File.Exists(sourceBgPath))
                {
                    return null;
                }

                ResizeImageAndSave(sourceBgPath, guidStr, height: 200);
                await _playerData.TrySetMapThumbAsync(dataModel.BeatmapDbId, guidStr);
                return guidStr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating beatmap thumb cache: {Identity}", dataModel.GetIdentity());
                return null;
            }
            finally
            {
                _lock.Release();
            }
        });
    }

    private static void ResizeImageAndSave(string sourcePath, string targetName, int width = 0, int height = 0)
    {
        var imageBytes = LoadImageData(sourcePath);
        var bitmapSource = CreateImage(imageBytes, width, height);
        imageBytes = GetEncodedImageData(bitmapSource, ".jpg");
        SaveImageData(imageBytes, Path.Combine(AppPaths.Current.ThumbCachePath, $"{targetName}.jpg"));
    }

    private static byte[] LoadImageData(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        return br.ReadBytes((int)fs.Length);
    }

    private static void SaveImageData(byte[] imageData, string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);
        bw.Write(imageData);
    }

    private static BitmapSource CreateImage(byte[] imageData, int decodePixelWidth, int decodePixelHeight)
    {
        if (imageData == null) return null;

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

        result.StreamSource = new MemoryStream(imageData);
        result.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        result.CacheOption = BitmapCacheOption.Default;
        result.EndInit();
        return result;
    }

    private static byte[] GetEncodedImageData(BitmapSource source, string preferredFormat)
    {
        BitmapEncoder encoder = preferredFormat.ToLower() switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            ".png" => new PngBitmapEncoder(),
            ".tif" or ".tiff" => new TiffBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            ".wmp" => new WmpBitmapEncoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(preferredFormat))
        };

        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);
        var result = new byte[stream.Length];
        using var br = new BinaryReader(stream);
        br.Read(result, 0, (int)stream.Length);
        return result;
    }
}
