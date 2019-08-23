using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.OsuPlayer.Control.Notification;
using Milky.WpfApi;

namespace Milky.OsuPlayer
{
    public static class Notification
    {
        public static void Show(string content, string title = null)
        {
            Execute.ToUiThread(() =>
            {
                App.NotificationList?.Add(new NotificationOption
                {
                    Content = content,
                    Title = title
                });
            });
        }

        public static void Show(NotificationOption notification)
        {
            Execute.ToUiThread(() => { App.NotificationList?.Add(notification); });
        }
    }
}
