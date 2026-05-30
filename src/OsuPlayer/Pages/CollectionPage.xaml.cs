using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;
using Milky.OsuPlayer.UiComponents.PanelComponent;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer.Pages;

/// <summary>
/// CollectionPage.xaml 的交互逻辑
/// </summary>
public partial class CollectionPage : Page
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly Binding s_sourceBinding = new(nameof(CollectionPageViewModel.DisplayedBeatmaps))
    {
        Mode = BindingMode.OneWay
    };

    private readonly MainWindow _mainWindow;
    private readonly ObservablePlayController _controller;

    private bool _minimal;

    public CollectionPage(CollectionPageViewModel viewModel, ObservablePlayController controller)
    {
        _controller = controller;
        InitializeComponent();
        _mainWindow = (MainWindow)Application.Current.MainWindow;

        DataContext = ViewModel = viewModel;
    }

    public CollectionPageViewModel ViewModel { get; set; }
    public string Id { get; set; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var minimal = AppSettings.Default.Interface.MinimalMode;
        if (minimal != _minimal)
        {
            if (minimal)
            {
                MapCardList.ItemsSource = null;
                MapList.SetBinding(ItemsControl.ItemsSourceProperty, s_sourceBinding);
                MapCardList.Visibility = Visibility.Collapsed;
                MapList.Visibility = Visibility.Visible;
            }
            else
            {
                MapList.ItemsSource = null;
                MapCardList.SetBinding(ItemsControl.ItemsSourceProperty, s_sourceBinding);
                MapList.Visibility = Visibility.Collapsed;
                MapCardList.Visibility = Visibility.Visible;
            }

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
        FrontDialogOverlay.Default.ShowContent(new EditCollectionControl(ViewModel.CollectionInfo),
            DialogOptionFactory.EditCollectionOptions);
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender,
        VirtualizingGalleryRoutedEventArgs e)
    {
        var dataModel = ViewModel.DisplayedBeatmaps[e.Index];
        try
        {
            var fileName = await CommonUtils.GetThumbByBeatmapDbId(dataModel).ConfigureAwait(false);
            Execute.OnUiThread(() => dataModel.ThumbPath = Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg"));
        }
        catch (Exception ex)
        {
            s_logger.Error(ex, "Error while loading panel item.");
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