using System.Windows;

namespace Milky.OsuPlayer.UiComponents.ButtonComponent
{
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