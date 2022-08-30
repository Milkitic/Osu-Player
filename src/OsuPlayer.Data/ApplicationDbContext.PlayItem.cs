using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Utils;

namespace Milki.OsuPlayer.Data;

public sealed partial class ApplicationDbContext
{
    public async ValueTask<PlayItem> GetOrAddPlayItem(string standardizedPath)
    {
        var playItem = await PlayItems
            //.AsNoTracking()
            .Include(k => k.PlayItemDetail)
            .Include(k => k.PlayItemConfig)
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayLists)
            .Include(k => k.PlayListRelations)
            .FirstOrDefaultAsync(k => k.StandardizedPath == standardizedPath);

        if (playItem != null)
        {
            bool changed = false;
            if (playItem.PlayItemConfig == null)
            {
                playItem.PlayItemConfig = new PlayItemConfig();
                changed = true;
            }

            if (playItem.PlayItemAsset == null)
            {
                playItem.PlayItemAsset = new PlayItemAsset();
                changed = true;
            }

            if (changed)
            {
                await SaveChangesAsync();
            }

            //Entry(playItem).State = EntityState.Detached;
            return playItem;
        }

        var folder = PathUtils.GetFolder(standardizedPath);
        var entity = new PlayItem
        {
            StandardizedPath = standardizedPath,
            IsAutoManaged = false,
            StandardizedFolder = folder,
            PlayLists = new List<PlayList>(),
            PlayItemAsset = new PlayItemAsset(),
            PlayItemConfig = new PlayItemConfig(),
            PlayItemDetail = new PlayItemDetail()
            {
                Artist = "",
                ArtistUnicode = "",
                Title = "",
                TitleUnicode = "",
                Creator = "",
                Version = "",
                BeatmapFileName = "",
                Source = "",
                Tags = "",
                FolderName = folder,
                AudioFileName = ""
            },
        };

        PlayItems.Add(entity);
        await SaveChangesAsync();
        return entity;
    }

    public async ValueTask<PlayItem> GetPlayItemByDetail(PlayItemDetail playItemDetail, bool createExtraInfos)
    {
        if (!createExtraInfos)
        {
            return await PlayItems
                .AsNoTracking()
                .Include(k => k.PlayItemDetail)
                .Include(k => k.PlayItemConfig)
                .Include(k => k.PlayItemAsset)
                .Include(k => k.PlayLists)
                .Include(k => k.PlayListRelations)
                .FirstAsync(k => k.PlayItemDetailId == playItemDetail.Id);
        }

        var playItem = await PlayItems
            .Include(k => k.PlayItemDetail)
            .Include(k => k.PlayItemConfig)
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayLists)
            .Include(k => k.PlayListRelations)
            .FirstAsync(k => k.PlayItemDetailId == playItemDetail.Id);


        bool changed = false;
        if (playItem.PlayItemConfig == null)
        {
            playItem.PlayItemConfig = new PlayItemConfig();
            changed = true;
        }

        if (playItem.PlayItemAsset == null)
        {
            playItem.PlayItemAsset = new PlayItemAsset();
            changed = true;
        }

        if (changed)
        {
            await SaveChangesAsync();
        }

        Entry(playItem).State = EntityState.Detached;
        return playItem;
    }

    public async ValueTask<IReadOnlyList<PlayItem>> GetPlayItemsByFolderAsync(string standardizedFolder)
    {
        return await PlayItems
            .AsNoTracking()
            .Where(k => k.StandardizedFolder == standardizedFolder)
            .Include(k => k.PlayItemDetail)
            .Include(k => k.PlayItemConfig)
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayLists)
            .Include(k => k.PlayListRelations)
            .ToArrayAsync();
    }

    public async ValueTask<IReadOnlyList<PlayItemDetail>> GetPlayItemDetailsByFolderAsync(string standardizedFolder)
    {
        return await PlayItemDetails
            .AsNoTracking()
            .Where(k => k.FolderName == standardizedFolder)
            .ToArrayAsync();
    }

    public async ValueTask UpdateThumbPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.StandardizedPath);
        item.PlayItemAsset!.ThumbPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

    public async ValueTask UpdateVideoPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.StandardizedPath);
        item.PlayItemAsset!.VideoPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

    public async ValueTask UpdateStoryboardVideoPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.StandardizedPath);
        item.PlayItemAsset!.StoryboardVideoPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }


    public async ValueTask RemoveFolderAll()
    {
        PlayItems.RemoveRange(PlayItems.Where(k => !k.StandardizedPath.StartsWith("./")));
        await SaveChangesAsync();
    }
}