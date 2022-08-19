#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Coosu.Beatmap;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Data;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Shared.Utils;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.ViewModels;

namespace Milki.OsuPlayer.UserControls;

/// <summary>
/// SelectCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class SelectCollectionControl : UserControl
{
    private readonly SelectCollectionPageViewModel _viewModel;
    private readonly FrontDialogOverlay _overlay;

    public SelectCollectionControl(PlayItem playItem) : this(new[] { playItem })
    {
    }

    public SelectCollectionControl(IList<PlayItem> playItems)
    {
        DataContext = _viewModel = new SelectCollectionPageViewModel();
        _viewModel.PlayItems = playItems;
        InitializeComponent();
        _overlay = FrontDialogOverlay.Default.GetOrCreateSubOverlay();
    }

    private async void SelectCollectionControl_OnInitialized(object? sender, EventArgs e)
    {
        await RefreshList();
    }

    private void BtnAddCollection_Click(object sender, RoutedEventArgs e)
    {
        var addCollectionControl = new AddCollectionControl();
        _overlay.ShowContent(addCollectionControl, DialogOptionFactory.AddCollectionOptions, async (obj, args) =>
        {
            await using var dbContext = new ApplicationDbContext();
            await dbContext.AddPlayListAsync(addCollectionControl.CollectionName.Text);
            await SharedVm.Default.UpdatePlayListsAsync();

            await RefreshList();
        });
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        FrontDialogOverlay.Default.RaiseOk();
    }

    private async Task RefreshList()
    {
        await using var dbContext = new ApplicationDbContext();
        _viewModel.PlayLists = new ObservableCollection<PlayList>(await dbContext.GetPlayListsAsync());
    }

    public static async Task<bool> AddToCollectionAsync(PlayList playList, IList<PlayItem> beatmaps)
    {
        if (beatmaps.Count <= 0) return false;

        await using var dbContext = new ApplicationDbContext();
        if (string.IsNullOrEmpty(playList.ImagePath))
        {
            var osuSongDir = AppSettings.Default.GeneralSection.OsuSongDir;
            foreach (var beatmap in beatmaps)
            {
                var folder = PathUtils.GetFullPath(beatmap.StandardizedFolder, osuSongDir);
                var path = PathUtils.GetFullPath(beatmap.StandardizedPath, osuSongDir);
                try
                {
                    var osuFile = await OsuFile.ReadFromFileAsync(path, options =>
                    {
                        options.IncludeSection("Events");
                        options.IgnoreSample();
                        options.IgnoreStoryboard();
                    });
                    if (osuFile.Events?.BackgroundInfo == null)
                        continue;

                    var imagePath = Path.Combine(folder, osuFile.Events.BackgroundInfo.Filename);
                    if (!File.Exists(imagePath)) continue;

                    playList.ImagePath = imagePath;
                    await dbContext.UpdateAndSaveChangesAsync(playList, k => k.ImagePath);
                    break;
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }

        await dbContext.AddPlayItemsToPlayList(beatmaps, playList);
        if (playList.IsDefault)
        {
            var playerService = App.Current.ServiceProvider.GetService<PlayerService>()!;
            var loadContext = playerService.LastLoadContext;
            if (loadContext?.PlayItem != null && beatmaps.Any(k => loadContext.PlayItem.Id.Equals(k.Id)))
            {
                loadContext.IsPlayItemFavorite = true;
            }
        }

        return true;
    }
}