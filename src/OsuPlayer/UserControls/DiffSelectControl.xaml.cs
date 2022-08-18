using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using Milki.OsuPlayer.UiComponents.FrontDialogComponent;

namespace Milki.OsuPlayer.UserControls
{
    public class DiffSelectPageViewModel : VmBase
    {
        private ObservableCollection<Beatmap> _dataList;

        public ObservableCollection<Beatmap> DataList
        {
            get => _dataList;
            set
            {
                if (Equals(value, _dataList)) return;
                _dataList = value;
                OnPropertyChanged();
            }
        }

        public Action<Beatmap, CallbackObj> Callback { get; set; }

        public ICommand SelectCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    var selectedMap = DataList.FirstOrDefault(k => k.Version == (string)obj);
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
        public DiffSelectControl(IEnumerable<Beatmap> beatmaps, Action<Beatmap, CallbackObj> onSelect)
        {
            InitializeComponent();

            _viewModel = (DiffSelectPageViewModel)DataContext;
            _viewModel.DataList = new ObservableCollection<Beatmap>(beatmaps.OrderBy(k => k.GameMode));
            _viewModel.Callback = onSelect;
        }
    }
}
