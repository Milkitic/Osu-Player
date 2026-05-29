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
        var selectedMap = Entries.FirstOrDefault(k => k.Version == (string)obj);
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

/// <summary>
/// DiffSelectControl.xaml 的交互逻辑
/// </summary>
public partial class DiffSelectControl : UserControl
{
    private readonly DiffSelectPageViewModel _viewModel;

    public DiffSelectControl(IEnumerable<Beatmap> entries, Func<Beatmap, CallbackObj, Task> onSelect)
    {
        InitializeComponent();

        _viewModel = (DiffSelectPageViewModel)DataContext;
        _viewModel.Entries = new ObservableCollection<Beatmap>(entries.OrderBy(k => k.GameMode));
        _viewModel.Callback = onSelect;
    }
}