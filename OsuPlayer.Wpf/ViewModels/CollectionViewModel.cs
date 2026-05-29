using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UserControls;

namespace Milky.OsuPlayer.ViewModels;

public partial class CollectionViewModel : ObservableObject
{
    private readonly IPlayerDataService _playerData;

    public CollectionViewModel(IPlayerDataService playerData)
    {
        _playerData = playerData;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public int Index { get; set; }
    public string ImagePath { get; set; }
    public string Description { get; set; }
    public DateTime CreateTime { get; set; }
    public bool Locked { get; set; }

    [RelayCommand]
    private async Task SelectAsync(IList<Beatmap> entries)
    {
        if (entries == null) return;
        var col = await _playerData.GetCollectionByIdAsync(Id);
        if (col == null) return;
        await SelectCollectionControl.AddToCollectionAsync(col, entries);
    }

    public static CollectionViewModel CopyFrom(Collection collection)
        => new(App.Services.GetRequiredService<IPlayerDataService>())
        {
            Id = collection.Id,
            Name = collection.Name,
            Index = collection.Index,
            ImagePath = collection.ImagePath,
            Description = collection.Description,
            CreateTime = collection.CreateTime,
            Locked = collection.LockedBool,
        };

    public static IEnumerable<CollectionViewModel> CopyFrom(IEnumerable<Collection> collection)
        => collection.Select(CopyFrom);
}