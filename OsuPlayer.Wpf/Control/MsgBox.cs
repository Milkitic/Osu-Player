using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Control
{
    public static class MsgBox
    {
        public static MessageBoxResult Show(string messageBoxText)
        {
            return Show(messageBoxText, "", MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxImage icon)
        {
            return Show(null, messageBoxText, caption, button, icon);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return Show(owner, messageBoxText, "", MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return Show(owner, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button)
        {
            return Show(owner, messageBoxText, caption, button, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            MessageBoxWindow messageBox = null;
            try
            {
                messageBox = new MessageBoxWindow(messageBoxText, caption, button, icon);
                Border cover = null;
                if (owner != null)
                {
                    cover = (Border)owner.FindName("MsgBoxCover");
                    if (cover != null)
                        cover.Visibility = Visibility.Visible;
                    dynamic dynOwner = owner;
                    try
                    {
                        if (!dynOwner.IsClosed)
                        {
                            messageBox.Owner = owner;
                            messageBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }

                messageBox.ShowDialog();
                if (cover != null)
                    cover.Visibility = Visibility.Hidden;
                return messageBox.MessageBoxResult;
            }
            finally
            {
              
            }
        }
    }
}
