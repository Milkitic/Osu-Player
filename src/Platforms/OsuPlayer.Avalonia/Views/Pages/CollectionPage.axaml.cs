using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Playback;
using OsuPlayer.Shared;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.Pages;

public partial class CollectionPage : UserControl
{
    private readonly IBeatmapThumbnailService? _thumbnailService;
    private readonly ILogger<CollectionPage>? _logger;
    private readonly ObservablePlayController? _controller;

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
        _thumbnailService = thumbnailService;
        _logger = logger;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private async void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not CollectionPageViewModel viewModel)
        {
            return;
        }

        viewModel.IsMinimalMode = AppSettings.Default?.Interface.MinimalMode == true;
        await LoadThumbsAsync(viewModel);
        var current = viewModel.Beatmaps?.FirstOrDefault(k =>
            k.GetIdentity().Equals(_controller?.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
        if (current != null)
        {
            MapList.SelectedItem = current;
        }
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CollectionPageViewModel.DisplayedBeatmaps) && sender is CollectionPageViewModel viewModel)
        {
            await LoadThumbsAsync(viewModel);
        }
    }

    private async Task LoadThumbsAsync(CollectionPageViewModel viewModel)
    {
        if (_thumbnailService == null || viewModel.DisplayedBeatmaps == null)
        {
            return;
        }

        foreach (var dataModel in viewModel.DisplayedBeatmaps)
        {
            try
            {
                var fileName = await _thumbnailService.GetThumbByBeatmapDbIdAsync(dataModel).ConfigureAwait(false);
                Dispatcher.UIThread.Post(() => dataModel.ThumbPath = Path.Combine(AppPaths.Current.ThumbCachePath, $"{fileName}.jpg"));
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error while loading panel item.");
            }
        }
    }

    private async void MapList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (MapList.SelectedItem is BeatmapDataModel map && DataContext is CollectionPageViewModel viewModel)
        {
            await viewModel.DirectPlayAsync(map);
        }
    }
}
