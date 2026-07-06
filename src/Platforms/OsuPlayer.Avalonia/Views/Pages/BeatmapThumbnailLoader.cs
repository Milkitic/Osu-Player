using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Shared;

namespace OsuPlayer.Views.Pages;

internal sealed class BeatmapThumbnailLoader
{
    private readonly IBeatmapThumbnailService _thumbnailService;
    private readonly ILogger _logger;

    public BeatmapThumbnailLoader(IBeatmapThumbnailService thumbnailService, ILogger logger)
    {
        _thumbnailService = thumbnailService;
        _logger = logger;
    }

    public async Task LoadAsync(BeatmapDataModel dataModel)
    {
        if (!string.IsNullOrWhiteSpace(dataModel.ThumbPath))
        {
            return;
        }

        try
        {
            var fileName = await _thumbnailService.GetThumbByBeatmapDbIdAsync(dataModel);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                dataModel.ThumbPath = ResolveThumbPath(fileName);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error while loading panel item.");
        }
    }

    private static string ResolveThumbPath(string fileName)
    {
        if (Path.IsPathRooted(fileName))
        {
            return fileName;
        }

        var cacheFileName = Path.HasExtension(fileName) ? fileName : $"{fileName}.jpg";
        return Path.Combine(AppPaths.Current.ThumbCachePath, cacheFileName);
    }
}
