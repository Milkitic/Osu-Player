using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared;

namespace Milki.OsuPlayer.Data;

public sealed partial class ApplicationDbContext
{
    public async Task<PlayItem> GetOrAddPlayItem(string standardizedPath)
    {
        var playItem = await PlayItems
            .AsNoTracking()
            .Include(k => k.PlayItemDetail)
            .Include(k => k.PlayItemConfig)
            .Include(k => k.PlayItemAsset)
            .Include(k => k.PlayLists)
            .Include(k => k.PlayListRelations)
            .FirstOrDefaultAsync(k => k.Path == standardizedPath);

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

            Entry(playItem).State = EntityState.Detached;
            return playItem;
        }

        var folder = PathUtilities.GetFolder(standardizedPath);
        var entity = new PlayItem
        {
            Path = standardizedPath,
            IsAutoManaged = false,
            Folder = folder,
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

    public async Task<PlayItem> GetPlayItemByDetail(PlayItemDetail playItemDetail, bool createExtraInfos)
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

    public async Task<IReadOnlyList<PlayItemDetail>> GetPlayItemDetailsByFolderAsync(string standardizedFolder)
    {
        return await PlayItemDetails
            .AsNoTracking()
            .Where(k => k.FolderName == standardizedFolder)
            .ToArrayAsync();
    }

    public async Task UpdateThumbPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.Path);
        item.PlayItemAsset!.ThumbPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

    public async Task UpdateVideoPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.Path);
        item.PlayItemAsset!.VideoPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }

    public async Task UpdateStoryboardVideoPath(PlayItem playItem, string path)
    {
        var item = await GetOrAddPlayItem(playItem.Path);
        item.PlayItemAsset!.StoryboardVideoPath = path;
        Update(item.PlayItemAsset);
        await SaveChangesAsync();
    }
}