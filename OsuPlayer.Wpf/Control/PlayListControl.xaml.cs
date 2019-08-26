using System;
using System.Collections.Generic;
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
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Player;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Control
{
    public class PlayListControlVm : ViewModelBase
    {
        private PlayerList _playList;

        public PlayerList PlayList
        {
            get => _playList;
            set
            {
                _playList = value;
                OnPropertyChanged();
            }
        }
    }
    /// <summary>
    /// PlayListControl.xaml 的交互逻辑
    /// </summary>
    public partial class PlayListControl : UserControl
    {
        public PlayListControl()
        {
            InitializeComponent();
        }

        public PlayListControlVm ViewModel { get; set; }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            ViewModel = (PlayListControlVm)this.DataContext;
            ViewModel.PlayList = Services.Get<PlayerList>();
        }
    }
}
