using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.UiComponents.FrontDialogComponent;

namespace Milky.OsuPlayer.UserControls;

public partial class DiffSelectPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<Beatmap> Entries { get; set; }

    public Func<Beatmap, CallbackObj, Task> Callback { get; set; }

    [RelayCommand]
    private async Task SelectAsync(object obj)
    {
        if (obj is not Beatmap selectedMap) return;
        var callbackObj = new CallbackObj();
        if (Callback != null)
            await Callback(selectedMap, callbackObj);
        if (!callbackObj.Handled)
            FrontDialogOverlay.Default.RaiseCancel();
    }
}

public class CallbackObj
{
    public bool Handled { get; set; } = false;
}

public partial class DiffSelectControl : UserControl
{
    private readonly DiffSelectPageViewModel _viewModel;

    public DiffSelectControl(IEnumerable<Beatmap> entries, Func<Beatmap, CallbackObj, Task> onSelect)
    {
        InitializeComponent();

        _viewModel = (DiffSelectPageViewModel)DataContext;
        _viewModel.Entries = new ObservableCollection<Beatmap>(entries.OrderBy(k => k.GameMode).ThenBy(k => k.DiffSrNoneStandard));
        _viewModel.Callback = onSelect;
    }
}