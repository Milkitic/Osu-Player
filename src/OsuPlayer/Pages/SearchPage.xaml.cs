using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NLog;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Media.Audio;
using OsuPlayer.UiComponents.PanelComponent;
using OsuPlayer.ViewModels;

using Microsoft.Extensions.Logging;

namespace OsuPlayer.Pages;

/// <summary>
///     SearchPage.xaml 的交互逻辑
/// </summary>
public partial class SearchPage : Page
{
    private readonly ILogger<SearchPage> _logger;
    private readonly IBeatmapThumbnailService _thumbnailService;
    private static bool _minimal;

    private readonly ObservablePlayController _controller;
    private readonly IPlayerDataService _playerData;
    private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;

    public SearchPage(
        SearchPageViewModel viewModel,
        IPlayerDataService playerData,
        ObservablePlayController controller,
        IBeatmapThumbnailService thumbnailService,
        ILogger<SearchPage> logger)
    {
        ViewModel = viewModel;
        _playerData = playerData;
        _controller = controller;
        _thumbnailService = thumbnailService;
        _logger = logger;

        InitializeComponent();
        DataContext = ViewModel;
    }

    public SearchPageViewModel ViewModel { get; set; }

    public SearchPage Search(string keyword)
    {
        SearchBox.Text = keyword;
        return this;
    }

    private async void SearchPage_Initialized(object sender, EventArgs e)
    {
        await ViewModel.PlayListQueryAsync(0, false);
    }

    private async void SearchPage_Loaded(object sender, RoutedEventArgs e)
    {
        var minimal = AppSettings.Default.Interface.MinimalMode;
        ViewModel.IsMinimalMode = minimal;
        if (minimal != _minimal)
        {
            _minimal = minimal;
            await ViewModel.PlayListQueryAsync(0, false);
        }
    }

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
        _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
        ViewModel.ClearGalleryNotifications = () => _virtualizingGalleryWrapPanel.ClearNotificationCount();
    }

    private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender,
        ItemLoadedEventArgs e)
    {
        var dataModel = ViewModel.DisplayedMaps[e.Index];
        try
        {
            var fileName = await _thumbnailService.GetThumbByBeatmapDbIdAsync(dataModel);
            dataModel.ThumbPath = Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading panel item.");
        }
    }

    private async void ResultListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultList.SelectedItem is BeatmapDataModel map)
        {
            await ViewModel.DirectPlayAsync(map);
        }
    }
}
