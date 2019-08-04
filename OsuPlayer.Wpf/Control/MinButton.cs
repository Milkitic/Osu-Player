using System.Windows;

namespace Milky.OsuPlayer.Control
{
    public class MinButton : SystemButton
    {
        public MinButton()
        {
            this.Click += OnClick;
        }

        private void OnClick(object sender, RoutedEventArgs args)
        {
            if (HostWindow != null)
            {
                HostWindow.WindowState = WindowState.Minimized;
            }
        }
    }
    public class CloseButton : SystemButton
    {
        public CloseButton()
        {
            this.Click += OnClick;
        }

        private void OnClick(object sender, RoutedEventArgs args)
        {
            HostWindow?.Close();
        }
    }
}