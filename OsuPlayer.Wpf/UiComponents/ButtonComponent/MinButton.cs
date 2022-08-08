using System.Windows;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent
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
}