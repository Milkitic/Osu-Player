using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Coosu.Beatmap;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Core;
using OsuPlayer.Core.Services;
using OsuPlayer.Data.Models;
using OsuPlayer.Playback;
using OsuPlayer.Services;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.UserControls;

public partial class SelectCollectionControl : UserControl
{
    public event EventHandler? CloseRequested;

    private readonly IPlayerDataService? _playerData;
    private readonly SelectCollectionPageViewModel? _viewModel;

    public SelectCollectionControl()
    {
        if (App.Services != null)
        {
            _playerData = App.Services.GetService<IPlayerDataService>();
        }

        InitializeComponent();
        _viewModel = DataContext as SelectCollectionPageViewModel;
        _ = RefreshListAsync();
    }

    public SelectCollectionControl(Beatmap entry) : this(new List<Beatmap> { entry })
    {
    }

    public SelectCollectionControl(IList<Beatmap> entries) : this()
    {
        if (_viewModel != null) _viewModel.Entries = entries;
    }

    private async void BtnAddCollection_Click(object? sender, RoutedEventArgs e)
    {
        if (_playerData == null)
        {
            return;
        }

        await FrontDialogService.ShowAddCollectionAsync(this, _playerData, RefreshAllCollectionViewsAsync);
    }

    private async void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is Collection col && _viewModel != null && _playerData != null)
        {
            var entries = _viewModel.Entries;
            if (entries != null && entries.Count > 0)
            {
                await AddToCollectionAsync(col, entries);
            }
        }

        if (CloseRequested != null)
        {
            CloseRequested.Invoke(this, EventArgs.Empty);
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window win)
        {
            win.Close();
        }
    }

    public void SetEntries(IList<Beatmap> entries)
    {
        if (_viewModel != null)
        {
            _viewModel.Entries = entries;
        }
    }

    private async Task RefreshListAsync()
    {
        if (_viewModel == null || _playerData == null) return;
        var cols = await _playerData.GetCollectionsAsync();
        _viewModel.Collections = new ObservableCollection<Collection>(cols.OrderByDescending(k => k.CreateTime));
    }

    private async Task RefreshAllCollectionViewsAsync()
    {
        await RefreshListAsync();

        if (FrontDialogService.GetMainWindow() is { } mainWindow)
        {
            await mainWindow.UpdateCollectionsAsync();
        }
    }

    public static async Task<bool> AddToCollectionAsync(Collection col, IList<Beatmap> entries)
    {
        if (App.Services == null) return false;
        var controller = App.Services.GetRequiredService<ObservablePlayController>();
        var playerData = App.Services.GetRequiredService<IPlayerDataService>();
        if (entries is not { Count: > 0 }) return false;
        if (string.IsNullOrEmpty(col.ImagePath))
        {
            var first = entries[0];
            var dir = first.GetFolder(out var isFromDb, out var freePath);
            if (isFromDb && string.IsNullOrWhiteSpace(dir)) return false;
            var filePath = isFromDb ? Path.Combine(dir!, first.BeatmapFileName) : freePath;
            try
            {
                var osuFile = await OsuFile.ReadFromFileAsync(filePath, options =>
                {
                    options.IncludeSection("Events");
                    options.IgnoreSample();
                    options.IgnoreStoryboard();
                });
                if (osuFile.Events.BackgroundInfo != null)
                {
                    var imgPath = Path.Combine(dir!, osuFile.Events.BackgroundInfo.Filename);
                    if (File.Exists(imgPath))
                    {
                        col.ImagePath = imgPath;
                        if (!await playerData.TryUpdateCollectionAsync(col))
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (!await playerData.TryAddMapsToCollectionAsync(entries, col))
        {
            return false;
        }

        var currentInfo = controller.PlayList?.CurrentInfo;
        if (!col.LockedBool || currentInfo?.Beatmap == null)
        {
            return true;
        }

        foreach (var beatmap in entries)
        {
            if (!currentInfo.Beatmap.GetIdentity().Equals(beatmap.GetIdentity())) continue;
            if (currentInfo.BeatmapDetail?.Metadata != null)
            {
                currentInfo.BeatmapDetail.Metadata.IsFavorite = false;
            }

            break;
        }

        return true;
    }
}
