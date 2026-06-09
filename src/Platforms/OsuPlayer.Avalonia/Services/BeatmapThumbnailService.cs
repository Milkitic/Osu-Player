using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coosu.Beatmap;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace OsuPlayer.Avalonia.Services;

public sealed class BeatmapThumbnailService : IBeatmapThumbnailService
{
    private readonly ILogger<BeatmapThumbnailService> _logger;
    private readonly IPlayerDataStore _playerData;
    private readonly SemaphoreSlim _lock = new(5);

    public BeatmapThumbnailService(IPlayerDataStore playerData, ILogger<BeatmapThumbnailService> logger)
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
                if (isFromDb && string.IsNullOrWhiteSpace(folder))
                {
                    return null!;
                }

                var osuFilePath = isFromDb ? Path.Combine(folder!, dataModel.BeatmapFileName) : freePath;

                if (!File.Exists(osuFilePath))
                {
                    return null!;
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
                    return null!;
                }

                var sourceBgPath = Path.Combine(folder, sourceBgFile);

                if (!File.Exists(sourceBgPath))
                {
                    return null!;
                }

                ResizeImageAndSave(sourceBgPath, guidStr, height: 200);
                await _playerData.TrySetMapThumbAsync(dataModel.BeatmapDbId, guidStr);
                return guidStr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating beatmap thumb cache: {Identity}", dataModel.GetIdentity());
                return null!;
            }
            finally
            {
                _lock.Release();
            }
        });
    }

    private static void ResizeImageAndSave(string sourcePath, string targetName, int width = 0, int height = 0)
    {
        using var image = Image.Load(sourcePath);
        if (width > 0 || height > 0)
        {
            var targetWidth = width > 0 ? width : image.Width;
            var targetHeight = height > 0 ? height : image.Height;
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(targetWidth, targetHeight),
                Mode = ResizeMode.Stretch
            }));
        }
        var target = Path.Combine(AppPaths.Current.ThumbCachePath, $"{targetName}.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        image.SaveAsJpeg(target);
    }
}
