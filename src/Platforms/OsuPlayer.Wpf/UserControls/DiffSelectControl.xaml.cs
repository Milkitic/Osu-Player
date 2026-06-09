using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Data.Models;

namespace OsuPlayer.UserControls;

public partial class DiffSelectPageViewModel : ObservableObject
{
    public event EventHandler<Beatmap> BeatmapSelected;

    [ObservableProperty]
    public partial ObservableCollection<Beatmap> Entries { get; set; }

    [RelayCommand]
    private void Select(object obj)
    {
        if (obj is not Beatmap selectedMap) return;
        BeatmapSelected?.Invoke(this, selectedMap);
    }
}

public partial class DiffSelectControl : UserControl
{
    public event EventHandler<Beatmap> BeatmapSelected;

    private readonly DiffSelectPageViewModel _viewModel;

    public DiffSelectControl(IEnumerable<Beatmap> entries)
    {
        InitializeComponent();

        _viewModel = (DiffSelectPageViewModel)DataContext;
        _viewModel.BeatmapSelected += (_, beatmap) => BeatmapSelected?.Invoke(this, beatmap);
        _viewModel.Entries = new ObservableCollection<Beatmap>(entries.OrderBy(k => k.GameMode).ThenBy(k => k.DiffSrNoneStandard));
    }
}
