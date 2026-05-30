using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper.FluentMap.Mapping;

namespace Milky.OsuPlayer.Data.Models;

public class CollectionMap : EntityMap<Collection>
{
    public CollectionMap()
    {
        Map(p => p.Id).ToColumn("id");
        Map(p => p.Name).ToColumn("name");
        Map(p => p.Locked).ToColumn("is_locked");
        Map(p => p.Index).ToColumn("sort_order");
        Map(p => p.ImagePath).ToColumn("cover_image_path");
        Map(p => p.Description).ToColumn("description");
        Map(p => p.CreateTime).ToColumn("created_at");
    }
}

public partial class Collection : ObservableObject
{
    public Collection()
    {
    }

    public Collection(string id, string name, bool locked, int index, string imagePath = null,
        string description = null)
    {
        Id = id;
        Name = name;
        Locked = locked ? 1 : 0;
        Index = index;
        ImagePath = imagePath;
        Description = description;
    }

    public string Id { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; }

    public int Locked { get; set; }

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    public partial string ImagePath { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial DateTime CreateTime { get; set; }

    public bool LockedBool => Locked == 1;
}