using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Controls.PanelComponent;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.Pages;

public partial class SearchPage : UserControl
{
    private readonly BeatmapThumbnailLoader? _thumbnailLoader;
    private VirtualizingGalleryWrapPanel? _virtualizingGalleryWrapPanel;
    private bool _initialized;

    public SearchPage()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    public SearchPage(
        SearchPageViewModel viewModel,
        IBeatmapThumbnailService thumbnailService,
        ILogger<SearchPage> logger) : this()
    {
        DataContext = viewModel;
        _thumbnailLoader = new BeatmapThumbnailLoader(thumbnailService, logger);
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not SearchPageViewModel viewModel)
        {
            return;
        }

        viewModel.IsMinimalMode = AppSettings.Default?.Interface.MinimalMode == true;
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await viewModel.PlayListQueryAsync(0, false);
    }

    private void Panel_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is VirtualizingGalleryWrapPanel panel)
        {
            _virtualizingGalleryWrapPanel = panel;
        }

        if (DataContext is SearchPageViewModel viewModel)
        {
            viewModel.ClearGalleryNotifications = () => _virtualizingGalleryWrapPanel?.ClearNotificationCount();
        }
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object? sender, ItemLoadedEventArgs e)
    {
        if (_thumbnailLoader == null ||
            DataContext is not SearchPageViewModel viewModel ||
            e.Index < 0 ||
            e.Index >= viewModel.DisplayedMaps.Count)
        {
            return;
        }

        var dataModel = viewModel.DisplayedMaps[e.Index];
        await _thumbnailLoader.LoadAsync(dataModel);
    }

    private async void ResultList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ResultList.SelectedItem is BeatmapDataModel map && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.DirectPlayAsync(map);
        }
    }

    private async void PlayWithDifficulty_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.PlayCommand.ExecuteAsync(map);
        }
    }

    private void SearchByCondition_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: BeatmapDataModel map, CommandParameter: string field } ||
            DataContext is not SearchPageViewModel viewModel)
        {
            return;
        }

        var keyword = field switch
        {
            "Title" => map.AutoTitle,
            "Artist" => map.AutoArtist,
            "Source" => map.SongSource,
            "Creator" => map.Creator,
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            viewModel.SearchByConditionCommand.Execute(keyword);
        }
    }

    private async void SaveCollection_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.SaveCollectionCommand.ExecuteAsync(map);
        }
    }

    private async void ExportCard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.ExportCommand.ExecuteAsync(map);
        }
    }

    private async void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.OpenSourceFolderCommand.ExecuteAsync(map);
        }
    }

    private async void OpenScorePage_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.OpenScorePageCommand.ExecuteAsync(map);
        }
    }

    private void PageBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: int index } && DataContext is SearchPageViewModel viewModel)
        {
            viewModel.SwitchCommand.Execute(index);
        }
    }

    private void PreviousPage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchPageViewModel viewModel)
        {
            viewModel.SwitchCommand.Execute(false);
        }
    }

    private void NextPage_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SearchPageViewModel viewModel)
        {
            viewModel.SwitchCommand.Execute(true);
        }
    }

}
