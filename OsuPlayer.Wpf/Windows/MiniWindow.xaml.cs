using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Presentation;
using System.Windows;

namespace Milky.OsuPlayer.Windows
{
    /// <summary>
    /// MiniWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MiniWindow : WindowEx
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
