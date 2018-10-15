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
    /// NewVersionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewVersionWindow : Window
    {
        private readonly Release _release;
        public bool IsClosed { get; private set; }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }

        public NewVersionWindow(Release release)
        {
            _release = release;
            InitializeComponent();
            MainGrid.DataContext = _release;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateWindow updateWindow = new UpdateWindow(_release);
            updateWindow.Show();
            Close();
        }
    }
}
