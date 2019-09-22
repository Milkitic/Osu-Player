using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Common.Metadata;
using Milky.OsuPlayer.Control.FrontDialog;
using Milky.WpfApi;
using Milky.WpfApi.Collections;
using Milky.WpfApi.Commands;

namespace Milky.OsuPlayer.Control
{
    public class DiffSelectPageViewModel : ViewModelBase
    {
        private ObservableCollection<Beatmap> _entries;

        public ObservableCollection<Beatmap> Entries
        {
            get => _entries;
            set
            {
                _entries = value;
                OnPropertyChanged();
            }
        }

        public Action<Beatmap> Callback { get; set; }

        public ICommand SelectCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    var selectedMap = Entries.FirstOrDefault(k => k.Version == (string)obj);
                    Callback?.Invoke(selectedMap);
                    FrontDialogOverlay.Default.RaiseCancel();
                });
            }
        }
    }

    /// <summary>
    /// DiffSelectControl.xaml 的交互逻辑
    /// </summary>
    public partial class DiffSelectControl : UserControl
    {
        private readonly DiffSelectPageViewModel _viewModel;
        public DiffSelectControl(IEnumerable<Beatmap> entries, Action<Beatmap> onSelect)
        {
            InitializeComponent();

            _viewModel = (DiffSelectPageViewModel)DataContext;
            _viewModel.Entries = new ObservableCollection<Beatmap>(entries.OrderBy(k => k.GameMode));
            _viewModel.Callback = onSelect;
        }
    }
}
