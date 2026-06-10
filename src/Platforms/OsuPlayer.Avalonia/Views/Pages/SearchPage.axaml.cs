using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Shared;
using OsuPlayer.ViewModels;

namespace OsuPlayer.Views.Pages;

public partial class SearchPage : UserControl
{
    private readonly IBeatmapThumbnailService? _thumbnailService;
    private readonly ILogger<SearchPage>? _logger;
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
        _thumbnailService = thumbnailService;
        _logger = logger;
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
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
            await LoadThumbsAsync(viewModel);
            return;
        }

        _initialized = true;
        await viewModel.PlayListQueryAsync(0, false);
        await LoadThumbsAsync(viewModel);
    }

    private async void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchPageViewModel.DisplayedMaps) && sender is SearchPageViewModel viewModel)
        {
            await LoadThumbsAsync(viewModel);
        }
    }

    private async Task LoadThumbsAsync(SearchPageViewModel viewModel)
    {
        if (_thumbnailService == null)
        {
            return;
        }

        foreach (var dataModel in viewModel.DisplayedMaps)
        {
            try
            {
                var fileName = await _thumbnailService.GetThumbByBeatmapDbIdAsync(dataModel);
                dataModel.ThumbPath = Path.Combine(AppPaths.Current.ThumbCachePath, $"{fileName}.jpg");
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error while loading panel item.");
            }
        }
    }

    private async void ResultList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ResultList.SelectedItem is BeatmapDataModel map && DataContext is SearchPageViewModel viewModel)
        {
            await viewModel.DirectPlayAsync(map);
        }
    }
}
