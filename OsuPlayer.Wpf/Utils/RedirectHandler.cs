using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Milkitic.OsuPlayer.Utils
{
    public static class RedirectHandler
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
