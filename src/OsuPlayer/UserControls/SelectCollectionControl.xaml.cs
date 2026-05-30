using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Coosu.Beatmap;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation;
using Milky.OsuPlayer.Presentation.Annotations;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.UserControls;

/// <summary>
/// SelectCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class SelectCollectionControl : UserControl
{
    private readonly IPlayerDataService _playerData;
    private readonly SelectCollectionPageViewModel _viewModel;
    private readonly FrontDialogOverlay _overlay;

    public SelectCollectionControl(Beatmap entry) : this([entry])
    {
    }

    public SelectCollectionControl(IList<Beatmap> entries)
    {
        if (App.Services != null)
        {
            _playerData = App.Services.GetRequiredService<IPlayerDataService>();
        }

        InitializeComponent();
        _viewModel = (SelectCollectionPageViewModel)DataContext;
        _viewModel.Entries = entries;
        _ = RefreshListAsync();
        _overlay = FrontDialogOverlay.Default.GetOrCreateSubOverlay();
    }

    private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
    {
        var addCollectionControl = new AddCollectionControl();
        _overlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions,
            (obj, args) => { _ = AddCollectionAndRefreshAsync(addCollectionControl.CollectionName.Text); });
    }

    private async Task AddCollectionAndRefreshAsync(string collectionName)
    {
        if (!await _playerData.TryAddCollectionAsync(collectionName, false))
            return;

        await WindowEx.GetCurrentFirst<MainWindow>().UpdateCollectionsAsync();
        await RefreshListAsync();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        FrontDialogOverlay.Default.RaiseOk();
    }

    private async Task RefreshListAsync()
    {
        _viewModel.Collections = new ObservableCollection<CollectionViewModel>(
            CollectionViewModel.CopyFrom(
                (await _playerData.GetCollectionsAsync()).OrderByDescending(k => k.CreateTime)));
    }

    public static async Task<bool> AddToCollectionAsync([NotNull] Collection col, IList<Beatmap> entries)
    {
        var controller = App.Services.GetRequiredService<ObservablePlayController>();
        var playerData = App.Services.GetRequiredService<IPlayerDataService>();
        if (entries is not { Count: > 0 }) return false;
        if (string.IsNullOrEmpty(col.ImagePath))
        {
            var first = entries[0];
            var dir = first.GetFolder(out var isFromDb, out var freePath);
            var filePath = isFromDb ? Path.Combine(dir, first.BeatmapFileName) : freePath;
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
                    var imgPath = Path.Combine(dir, osuFile.Events.BackgroundInfo.Filename);
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

        foreach (var beatmap in entries)
        {
            if (!controller.PlayList.CurrentInfo.Beatmap.GetIdentity().Equals(beatmap.GetIdentity()) ||
                !col.LockedBool) continue;
            controller.PlayList.CurrentInfo.BeatmapDetail.Metadata.IsFavorite = false;
            break;
        }

        return true;
    }
}