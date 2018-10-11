using Milkitic.OsuPlayer.Pages.Settings;
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

namespace Milkitic.OsuPlayer
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public bool IsClosed { get; private set; }

        public ConfigWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }

        private void BtnGeneral_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new GeneralPage());
        }

        private void BtnHotkey_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HotKeyPage());
        }

        private void BtnLyric_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LyricPage());
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ExportPage());
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AboutPage());
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PlayPage());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BtnGeneral_Click(sender, e);
        }
    }
}
