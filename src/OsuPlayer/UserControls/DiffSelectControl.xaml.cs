using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UserControls;

public class DiffSelectPageViewModel : VmBase
{
    private ObservableCollection<PlayItem> _dataList;

    public ObservableCollection<PlayItem> DataList
    {
        get => _dataList;
        set => this.RaiseAndSetIfChanged(ref _dataList, value);
    }

    public Action<PlayItem, CallbackObj> Callback { get; set; }

    public ICommand SelectCommand
    {
        get
        {
            return new DelegateCommand(obj =>
            {
                var selectedMap = DataList.FirstOrDefault(k => k.PlayItemDetail.Version == (string)obj);
                var callbackObj = new CallbackObj();
                Callback?.Invoke(selectedMap, callbackObj);
                if (!callbackObj.Handled)
                    FrontDialogOverlay.Default.RaiseCancel();
            });
        }
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
    public DiffSelectControl(IEnumerable<PlayItem> beatmaps, Action<PlayItem, CallbackObj> onSelect)
    {
        InitializeComponent();

        DataContext = _viewModel = new DiffSelectPageViewModel();
        _viewModel.DataList = new ObservableCollection<PlayItem>(beatmaps.OrderBy(k => k.PlayItemDetail.GameMode));
        _viewModel.Callback = onSelect;
    }
}