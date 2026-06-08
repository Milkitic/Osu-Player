using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using OsuPlayer.Core;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Core.Services;
using OsuPlayer.Playback;
using OsuPlayer.Presentation.Interaction;
using OsuPlayer.Shared;
using OsuPlayer.UiComponents.FrontDialogComponent;
using OsuPlayer.UiComponents.PanelComponent;
using OsuPlayer.UserControls;
using OsuPlayer.Utils;
using OsuPlayer.ViewModels;
using OsuPlayer.Windows;

namespace OsuPlayer.Pages;

/// <summary>
/// CollectionPage.xaml 的交互逻辑
/// </summary>
public partial class CollectionPage : Page
{
    private readonly ILogger<CollectionPage> _logger;
    private readonly IBeatmapThumbnailService _thumbnailService;
    private readonly MainWindow _mainWindow;
    private readonly ObservablePlayController _controller;

    private bool _minimal;

    public CollectionPage(
        CollectionPageViewModel viewModel,
        ObservablePlayController controller,
        IBeatmapThumbnailService thumbnailService,
        ILogger<CollectionPage> logger)
    {
        _controller = controller;
        _thumbnailService = thumbnailService;
        _logger = logger;
        InitializeComponent();
        _mainWindow = (MainWindow)Application.Current.MainWindow;

        DataContext = ViewModel = viewModel;
    }

    public CollectionPageViewModel ViewModel { get; set; }
    public string Id { get; set; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var minimal = AppSettings.Default.Interface.MinimalMode;
        ViewModel.IsMinimalMode = minimal;
        if (minimal != _minimal)
        {
            _minimal = minimal;
        }

        var item = ViewModel.Beatmaps?.FirstOrDefault(k =>
            k.GetIdentity().Equals(_controller.PlayList.CurrentInfo?.Beatmap?.GetIdentity()));
        if (item != null)
            MapList.SelectedItem = item;
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    private void Dispose()
    {
        // todo
    }

    private async void BtnDelCol_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(_mainWindow, I18NUtil.GetString("ui-ensureRemoveCollection"),
            _mainWindow.Title, MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result == MessageBoxResult.OK)
        {
            await ViewModel.DeleteCollectionAsync();
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.CollectionInfo, ViewModel.PlayerData),
            DialogOptionFactory.EditCollectionOptions);
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender,
        ItemLoadedEventArgs e)
    {
        var dataModel = ViewModel.DisplayedBeatmaps[e.Index];
        try
        {
            var fileName = await _thumbnailService.GetThumbByBeatmapDbIdAsync(dataModel).ConfigureAwait(false);
            Execute.OnUiThread(() => dataModel.ThumbPath = Path.Combine(AppPaths.Current.ThumbCachePath, $"{fileName}.jpg"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while loading panel item.");
        }
    }

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private async void MapListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        if (MapList.SelectedItem is BeatmapDataModel map)
        {
            await ViewModel.DirectPlayAsync(map);
        }
    }
}
