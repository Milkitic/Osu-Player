using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Views.UserControls;

public partial class DiffSelectPageViewModel : ObservableObject
{
    public event EventHandler<Beatmap>? BeatmapSelected;

    [ObservableProperty]
    private ObservableCollection<Beatmap> _entries = new();

    [RelayCommand]
    private void Select(object? obj)
    {
        if (obj is not Beatmap selectedMap) return;
        BeatmapSelected?.Invoke(this, selectedMap);
    }
}

public partial class DiffSelectControl : UserControl
{
    public event EventHandler<Beatmap>? BeatmapSelected;

    private readonly DiffSelectPageViewModel _viewModel = new();

    public DiffSelectControl()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.BeatmapSelected += (_, beatmap) => BeatmapSelected?.Invoke(this, beatmap);
    }

    public DiffSelectControl(IEnumerable<Beatmap> entries) : this()
    {
        _viewModel.Entries = new ObservableCollection<Beatmap>(entries.OrderBy(k => k.GameMode).ThenBy(k => k.DiffSrNoneStandard));
    }
}
