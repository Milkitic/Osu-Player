using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Controls.PanelComponent;
using OsuPlayer.Playback;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.Pages;

public partial class CollectionPage : UserControl
{
    private readonly BeatmapThumbnailLoader? _thumbnailLoader;
    private readonly ObservablePlayController? _controller;
    private VirtualizingGalleryWrapPanel? _virtualizingGalleryWrapPanel;

    public CollectionPage()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    public CollectionPage(
        CollectionPageViewModel viewModel,
        ObservablePlayController controller,
        IBeatmapThumbnailService thumbnailService,
        ILogger<CollectionPage> logger) : this()
    {
        DataContext = viewModel;
        _controller = controller;
        _thumbnailLoader = new BeatmapThumbnailLoader(thumbnailService, logger);
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not CollectionPageViewModel viewModel)
        {
            return;
        }

        viewModel.IsMinimalMode = AppSettings.Default?.Interface.MinimalMode == true;
        var current = viewModel.Beatmaps?.FirstOrDefault(k =>
            k.GetIdentity().Equals(_controller?.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
        if (current != null)
        {
            MapList.SelectedItem = current;
            MapCardList.SelectedItem = current;
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CollectionPageViewModel.DisplayedBeatmaps))
        {
            _virtualizingGalleryWrapPanel?.ClearNotificationCount();
        }
    }

    private void Panel_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is VirtualizingGalleryWrapPanel panel)
        {
            _virtualizingGalleryWrapPanel = panel;
        }
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object? sender, ItemLoadedEventArgs e)
    {
        if (_thumbnailLoader == null ||
            DataContext is not CollectionPageViewModel viewModel ||
            viewModel.DisplayedBeatmaps == null ||
            e.Index < 0 ||
            e.Index >= viewModel.DisplayedBeatmaps.Count)
        {
            return;
        }

        var dataModel = viewModel.DisplayedBeatmaps[e.Index];
        await _thumbnailLoader.LoadAsync(dataModel);
    }

    private async void MapList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.DirectPlayAsync(map);
        }
    }

    private async void PlayWithDifficulty_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.PlayCommand.ExecuteAsync(map);
        }
    }

    private void SearchByCondition_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: BeatmapDataModel map, CommandParameter: string field } ||
            DataContext is not CollectionPageViewModel viewModel)
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
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.SaveCollectionCommand.ExecuteAsync(map);
        }
    }

    private async void ExportCard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.ExportCommand.ExecuteAsync(map);
        }
    }

    private async void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.OpenSourceFolderCommand.ExecuteAsync(map);
        }
    }

    private async void OpenScorePage_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.OpenScorePageCommand.ExecuteAsync(map);
        }
    }

    private async void RemoveMap_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: BeatmapDataModel map } && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.RemoveCommand.ExecuteAsync(map);
        }
    }

    private async void MapList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete ||
            sender is not ListBox listBox ||
            listBox.SelectedItems is not { Count: > 0 } selectedItems ||
            DataContext is not CollectionPageViewModel viewModel)
        {
            return;
        }

        e.Handled = true;
        await viewModel.RemoveSelectedCommand.ExecuteAsync(selectedItems);
    }

}
