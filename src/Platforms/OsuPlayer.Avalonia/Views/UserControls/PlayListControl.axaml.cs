using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OsuPlayer.Data.Models;
using OsuPlayer.Playback;
using OsuPlayer.Services;

namespace OsuPlayer.Views.UserControls;

public partial class PlayListControlVm : ObservableObject
{
    public ObservablePlayController? Controller { get; }
    private readonly IExportService? _exportService;

    public PlayListControlVm()
    {
        if (App.Services != null)
        {
            Controller = App.Services.GetService<ObservablePlayController>();
            _exportService = App.Services.GetService<IExportService>();
        }
    }

    [ObservableProperty]
    private Beatmap? _selectedMap;

    [ObservableProperty]
    private List<Beatmap> _selectedMaps = new();

    [RelayCommand]
    private async Task ClearPlayListAsync()
    {
        if (Controller == null) return;
        await Controller.SetPlaylistAsync(Array.Empty<Beatmap>(), false);
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (Controller == null || SelectedMap == null) return;
        await Controller.PlayNewAsync(SelectedMap);
    }

    [RelayCommand]
    private void Search(string param)
    {
        if (SelectedMap == null) return;
        var keyword = param switch
        {
            "0" => SelectedMap.AutoTitle,
            "1" => SelectedMap.AutoArtist,
            "2" => SelectedMap.SongSource,
            "3" => SelectedMap.Creator,
            _ => null
        };
        if (!string.IsNullOrEmpty(keyword))
        {
            AppNotificationService.Instance.Push($"搜索: {keyword}");
        }
    }

    [RelayCommand]
    private void OpenSourceFolder()
    {
        AppNotificationService.Instance.Push("ui-ctxMenu-openSourceFolder");
    }

    [RelayCommand]
    private void OpenScorePage()
    {
        AppNotificationService.Instance.Push("ui-ctxMenu-openScorePage");
    }

    [RelayCommand]
    private async Task SaveCollectionAsync()
    {
        if (SelectedMap == null) return;
        await FrontDialogService.ShowSelectCollectionAsync(null, SelectedMap);
    }

    [RelayCommand]
    private async Task SaveAllCollectionAsync()
    {
        if (Controller?.PlayList.SongList is not { Count: > 0 } entries) return;
        await FrontDialogService.ShowSelectCollectionAsync(null, entries);
    }

    [RelayCommand]
    private void Export()
    {
        _exportService?.QueueEntry(SelectedMap!);
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (Controller == null) return;
        await Controller.RemoveFromPlaylistAsync(SelectedMaps);
    }
}

public partial class PlayListControl : UserControl
{
    public event EventHandler? CloseRequested;

    public PlayListControl()
    {
        DataContext = new PlayListControlVm();
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PlayList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is PlayListControlVm vm && sender is ListBox lb && lb.SelectedItem is Beatmap bm)
        {
            vm.SelectedMap = bm;
            vm.SelectedMaps = new List<Beatmap> { bm };
        }
    }

    private void PlayList_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is PlayListControlVm vm)
        {
            vm.PlayCommand.Execute(null);
        }
    }
}
