using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Milky.OsuPlayer.Media.Storyboard;

namespace Milky.OsuPlayer
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

        public bool IsClosed { get; set; }

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

        private void Window_Closed(object sender, EventArgs e)
        {
            App.StoryboardProvider.Dispose();
            IsClosed = true;
        }
    }
}
