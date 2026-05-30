using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UiComponents.PanelComponent;
using Milky.OsuPlayer.ViewModels;
using NLog;

namespace Milky.OsuPlayer.Pages;

/// <summary>
///     SearchPage.xaml 的交互逻辑
/// </summary>
public partial class SearchPage : Page
{
    private static readonly Logger s_logger = LogManager.GetCurrentClassLogger();
    private static readonly Binding s_sourceBinding = new(nameof(SearchPageViewModel.DisplayedMaps))
    {
        Mode = BindingMode.OneWay
    };

    private static bool _minimal;

    private readonly ObservablePlayController _controller;
    private readonly IPlayerDataService _playerData;
    private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;

    public SearchPage(SearchPageViewModel viewModel, IPlayerDataService playerData, ObservablePlayController controller)
    {
        ViewModel = viewModel;
        _playerData = playerData;
        _controller = controller;

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
        if (minimal != _minimal)
        {
            if (minimal)
            {
                ResultCardList.ItemsSource = null;
                ResultList.SetBinding(ItemsControl.ItemsSourceProperty, s_sourceBinding);
                ResultCardList.Visibility = Visibility.Collapsed;
                ResultList.Visibility = Visibility.Visible;
            }
            else
            {
                ResultList.ItemsSource = null;
                ResultCardList.SetBinding(ItemsControl.ItemsSourceProperty, s_sourceBinding);
                ResultList.Visibility = Visibility.Collapsed;
                ResultCardList.Visibility = Visibility.Visible;
            }

            _minimal = minimal;
            await ViewModel.PlayListQueryAsync(0, false);
        }
    }

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
        _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
        ViewModel.GalleryWrapPanel = _virtualizingGalleryWrapPanel;
    }

    private void BtnQueueAll_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void VirtualizingGalleryWrapPanel_OnItemLoaded(object sender,
        VirtualizingGalleryRoutedEventArgs e)
    {
        var dataModel = ViewModel.DisplayedMaps[e.Index];
        try
        {
            var fileName = await CommonUtils.GetThumbByBeatmapDbId(dataModel);
            dataModel.ThumbPath = Path.Combine(Domain.ThumbCachePath, $"{fileName}.jpg");
        }
        catch (Exception ex)
        {
            s_logger.Error(ex, "Error while loading panel item.");
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