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
using System.Windows.Shapes;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.ViewModels;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MiniWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MiniWindow : WindowBase
    {
        public MiniWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
             var s = AppSettings.Default.General.MiniPosition;
            if (s != null && s.Length == 2)
            {
                Left = s[0];
                Top = s[1];
            }
            else
            {
                Left = SystemParameters.PrimaryScreenWidth - this.ActualWidth - 20;
                Top = SystemParameters.PrimaryScreenHeight - this.ActualHeight - 100;
            }
        }

        private void ControlMaxButtonClicked()
        { 
          
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppSettings.Default.General.MiniPosition = new[] { Left, Top };
            AppSettings.SaveDefault();
        }
    }
}
