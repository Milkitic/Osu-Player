using Milkitic.OsuPlayer.Wpf.Storyboard;
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

namespace Milkitic.OsuPlayer.Wpf
{
    /// <summary>
    /// StoryboardWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StoryboardWindow : Window
    {
        public StoryboardWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.StoryboardProvider = new StoryboardProvider(this);
            AdjustOffset();
            CompositionTarget.Rendering += OnRendering;
        }

        private void AdjustOffset()
        {
            //Width += 14;
            //Height += 14;
            //Width += 16;
            //Height += 39;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized) return;
            if (!App.StoryboardProvider.HwndRenderBase.DisposeRequested)
                App.StoryboardProvider.HwndRenderBase.UpdateFrame();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
            if (e.RightButton == MouseButtonState.Pressed)
                this.Hide();
        }
    }
}
