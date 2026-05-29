using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.Shared.Dependency;
using Milky.OsuPlayer.UiComponents.PanelComponent;
using Milky.OsuPlayer.ViewModels;
using Milky.OsuPlayer.Windows;
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

    private readonly ObservablePlayController _controller = Service.Get<ObservablePlayController>();
    private readonly IPlayerDataService _playerData = AppServices.PlayerData;

    private MainWindow _mainWindow;
    private VirtualizingGalleryWrapPanel _virtualizingGalleryWrapPanel;

    public SearchPage()
    {
        _mainWindow = (MainWindow)Application.Current.MainWindow;

        InitializeComponent();
    }

    public SearchPageViewModel ViewModel { get; set; }

    public SearchPage Search(string keyword)
    {
        SearchBox.Text = keyword;
        return this;
    }

    private async void SearchPage_Initialized(object sender, EventArgs e)
    {
        ViewModel = (SearchPageViewModel)DataContext;
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

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.SearchText = ((TextBox)sender).Text;
        await ViewModel.PlayListQueryAsync(0);
    }

    private void Panel_Loaded(object sender, RoutedEventArgs e)
    {
        _virtualizingGalleryWrapPanel = sender as VirtualizingGalleryWrapPanel;
        ViewModel.GalleryWrapPanel = _virtualizingGalleryWrapPanel;
    }

    private void ResultListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PlaySelectedDefault();
    }

    private async void PlaySelectedDefault()
    {
        var map = await GetSelectedDefaultAsync();
        if (map == null)
            return;
        //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
        //    map.BeatmapFileName));
        await _controller.PlayNewAsync(map);
    }

    private async Task<Beatmap> GetSelectedDefaultAsync()
    {
        if (ResultList.SelectedItem == null)
            return null;
        var map = (await _playerData
                .GetBeatmapsFromFolderAsync(((BeatmapDataModel)ResultList.SelectedItem).FolderName))
            .GetHighestDiff();
        return map;
    }

    private async void BtnPlayAll_Click(object sender, RoutedEventArgs e)
    {
        var beatmaps = await ViewModel.GetAllMatchedBeatmapsAsync();
        if (beatmaps.Count <= 0) return;
        var group = beatmaps.GroupBy(k => k.FolderName);
        var newBeatmaps = group
            .Select(k => k.GetHighestDiff())
            .ToList();

        //if (map == null) return;
        //await _mainWindow.PlayNewFile(Path.Combine(Domain.OsuSongPath, map.FolderName,
        //     map.BeatmapFileName));
        await _controller.PlayList.SetSongListAsync(newBeatmaps, true);
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
}