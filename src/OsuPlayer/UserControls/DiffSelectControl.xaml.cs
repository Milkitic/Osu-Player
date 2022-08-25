using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UserControls;

public class DiffSelectControlVm : VmBase
{
    private ObservableCollection<PlayItem> _playItems;

    public ObservableCollection<PlayItem> PlayItems
    {
        get => _playItems;
        set => this.RaiseAndSetIfChanged(ref _playItems, value);
    }

    public Action<PlayItem, CallbackObj> Callback { get; set; }

    public ICommand SelectCommand => new DelegateCommand(obj =>
    {
        if (obj is not PlayItem playItem) return;
        var callbackObj = new CallbackObj();
        Callback?.Invoke(playItem, callbackObj);
        if (!callbackObj.Handled)
        {
            App.CurrentMainContentDialog.RaiseCancel();
        }
    });
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
    private readonly DiffSelectControlVm _viewModel;

    public DiffSelectControl(IEnumerable<PlayItem> playItems, Action<PlayItem, CallbackObj> onSelect)
    {
        InitializeComponent();

        DataContext = _viewModel = new DiffSelectControlVm();
        _viewModel.PlayItems = new ObservableCollection<PlayItem>(playItems
            .OrderBy(k => k.PlayItemDetail.GameMode)
            .ThenBy(k => k.PlayItemDetail.StarRating)
        );
        _viewModel.Callback = onSelect;
    }
}