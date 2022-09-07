#nullable enable

using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data.Models;

namespace Milki.OsuPlayer.Converters;

public class PathToCurrentPlayConverter : IValueConverter
{
    private readonly ConcurrentDictionary<string, LoosePlayItem?> _cache = new();

    public PathToCurrentPlayConverter()
    {
        Default = this;
    }

    public static PathToCurrentPlayConverter? Default { get; private set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s) return null;

        return GetLoosePlayItemByStandardizedPath(s, parameter as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object? GetLoosePlayItemByStandardizedPath(string standardizedPath, string? type)
    {
        if (_cache.TryGetValue(standardizedPath, out var playItem))
        {
            return ReturnObject(type, playItem);
        }

        using var appDbContext = ServiceProviders.GetApplicationDbContext();

        playItem = appDbContext.PlayItems.AsNoTracking()
            .Where(k => k.StandardizedPath == standardizedPath)
            .Select(k => new LoosePlayItem
            {
                Title = k.PlayItemDetail.AutoTitle,
                Artist = k.PlayItemDetail.AutoArtist,
                Creator = k.PlayItemDetail.Creator,
                Version = k.PlayItemDetail.Version,
                Id = k.PlayItemDetail.BeatmapId, // intended
                PlayItem = k,
                LooseItemType = LooseItemType.CurrentPlay
            }).FirstOrDefault();
        _cache.TryAdd(standardizedPath, playItem);

        return ReturnObject(type, playItem);
    }

    private static object? ReturnObject(string? type, LoosePlayItem? playItem)
    {
        return type switch
        {
            "Title" => playItem?.Title,
            "Artist" => playItem?.Artist,
            "Creator" => playItem?.Creator,
            "Version" => playItem?.Version,
            "Id" => playItem?.Id,
            _ => playItem
        };
    }
}