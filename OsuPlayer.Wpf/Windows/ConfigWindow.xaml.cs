using System.Windows;
using System.Windows.Controls.Primitives;
using Milky.OsuPlayer.Pages.Settings;
using Milky.OsuPlayer.Utils;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : WindowBase
    {
        public ConfigWindow()
        {
            InitializeComponent();
        }
        
        private void Window_Shown(object sender, System.EventArgs e)
        {
            SwitchGeneral.IsChecked = true;
        }
    }
}
