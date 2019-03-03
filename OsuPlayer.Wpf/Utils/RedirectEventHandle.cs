using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Utils
{
    public static class RedirectEventHandle
    {
        public static void Redirect()
        {
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.ContextMenuOpeningEvent,
                new RoutedEventHandler(OnContextMenuOpening));
        }

        private static void OnContextMenuOpening(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Border b)
            {
                if (b.Name == "SelectedItem")
                    return;
            }

            e.Handled = true;
        }
    }
}
