using System.Windows;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent
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