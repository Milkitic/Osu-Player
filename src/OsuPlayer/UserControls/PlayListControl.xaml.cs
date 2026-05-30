using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Milky.OsuPlayer.Core;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Media.Audio.Playlist;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Services;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;

namespace Milky.OsuPlayer.UserControls;

public partial class PlayListControlVm : ObservableObject
{
    public ObservablePlayController Controller { get; }
    private readonly IExportService _exportService;

    public PlayListControlVm(ObservablePlayController controller, IExportService exportService)
    {
        Controller = controller;
        _exportService = exportService;
    }

    [ObservableProperty]
    public partial Beatmap SelectedMap { get; set; }

    [ObservableProperty]
    public partial List<Beatmap> SelectedMaps { get; set; }

    [RelayCommand]
    private async Task ClearPlayListAsync()
    {
        await Controller.PlayList.SetSongListAsync(Array.Empty<Beatmap>(), false);
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        await Controller.PlayNewAsync(SelectedMap);
    }

    [RelayCommand]
    private void Search(object param)
    {
        var s = (string)param;
        string keyword = s switch
        {
            "0" => SelectedMap.AutoTitle,
            "1" => SelectedMap.AutoArtist,
            "2" => SelectedMap.SongSource,
            "3" => SelectedMap.Creator,
            _ => null
        };

        if (!string.IsNullOrEmpty(keyword))
        {
            WeakReferenceMessenger.Default.Send(new SearchRequestedMessage(keyword));
        }
    }

    [RelayCommand]
    private void OpenSourceFolder()
    {
        Process.Start(SelectedMap.GetFolder(out _, out _));
    }

    [RelayCommand]
    private void OpenScorePage()
    {
        Process.Start($"https://osu.ppy.sh/b/{SelectedMap.BeatmapId}");
    }

    [RelayCommand]
    private void SaveCollection()
    {
        FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(SelectedMap),
            DialogOptionFactory.SelectCollectionOptions);
    }

    [RelayCommand]
    private void SaveAllCollection()
    {
        if (Controller.PlayList.SongList.Count == 0) return;
        FrontDialogOverlay.Default.ShowContent(new SelectCollectionControl(Controller.PlayList.SongList),
            DialogOptionFactory.SelectCollectionOptions);
    }

    [RelayCommand]
    private void Export()
    {
        _exportService.QueueEntry(SelectedMap);
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        foreach (var beatmap in SelectedMaps)
        {
            Controller.PlayList.SongList.Remove(beatmap);
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// PlayListControl.xaml 的交互逻辑
/// </summary>
public partial class PlayListControl : UserControl
{
    public static readonly RoutedEvent CloseRequestedEvent =
        EventManager.RegisterRoutedEvent("CloseRequested",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(PlayListControl));

    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    private bool _signed;
    private readonly ObservablePlayController _controller;
    private PlayListControlVm _viewModel;

    public PlayListControl()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            DataContext = App.Services.GetRequiredService(typeof(PlayListControlVm));
            _controller = App.Services.GetRequiredService<ObservablePlayController>();
        }

        InitializeComponent();
    }

    private void UserControl_Initialized(object sender, EventArgs e)
    {
        _viewModel = (PlayListControlVm)DataContext;
        _signed = false;
    }

    private void PlayListItem_MouseDoubleClick(object sender, RoutedEventArgs e)
    {
        _viewModel.PlayCommand?.Execute(null);
    }

    private void PlayList_MouseMove(object sender, MouseEventArgs e)
    {
        //var item = Mouse.DirectlyOver;
        //if (item is Border b && b.Child is GridViewRowPresenter pre)
        //{
        //    var beatmap
        //}
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));
        //RaiseEvent(e);
    }

    private void PlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
            return;
        _viewModel.SelectedMap = (Beatmap)e.AddedItems[e.AddedItems.Count - 1];
        var selected = e.AddedItems;
        _viewModel.SelectedMaps = selected.Cast<Beatmap>().ToList();
    }

    private void PlayList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (PlayList.SelectedItems.Count == 0)
        {
            e.Handled = true;
            return;
        }

        _viewModel.SelectedMap = (Beatmap)PlayList.SelectedItems[PlayList.SelectedItems.Count - 1];
        _viewModel.SelectedMaps = PlayList.SelectedItems.Cast<Beatmap>().ToList();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_signed)
        {
            if (_controller != null)
                _controller.LoadStarted += Controller_LoadStarted;
            _signed = true;
        }

        if (_controller != null)
            PlayList.SelectedItem = _controller.PlayList.CurrentInfo?.Beatmap;
        if (PlayList.SelectedItem != null)
        {
            PlayList.ScrollIntoView(PlayList.SelectedItem);
            ListViewItem item =
                PlayList.ItemContainerGenerator.ContainerFromItem(PlayList.SelectedItem) as ListViewItem;
            item?.Focus();
        }
    }

    private void Controller_LoadStarted(BeatmapContext beatmapCtx, System.Threading.CancellationToken ct)
    {
        var info = _controller.PlayList.CurrentInfo?.Beatmap;
        if (info != null) PlayList.ScrollIntoView(info);
    }
}