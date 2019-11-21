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
using Milky.OsuPlayer.Common.Configuration;

namespace Milky.OsuPlayer.Pages.Settings
{
    /// <summary>
    /// InterfacePage.xaml 的交互逻辑
    /// </summary>
    public partial class InterfacePage : Page
    {
        public InterfacePage()
        {
            InitializeComponent();

            var @interface = AppSettings.Default.Interface;
            this.DataContext = @interface;
            @interface.PropertyChanged += interface_PropertyChanged;
        }

        private void interface_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AppSettings.SaveDefault();
        }
    }
}
