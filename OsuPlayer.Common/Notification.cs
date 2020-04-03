using Milky.OsuPlayer.Presentation.Interaction;
using System.Collections.ObjectModel;

namespace Milky.OsuPlayer.Common
{
    public static class Notification
    {
        public static ObservableCollection<NotificationOption> NotificationList { get; } =
            new ObservableCollection<NotificationOption>();

        public static void Push(string content, string title = null)
        {
            Execute.ToUiThread(() =>
            {
                NotificationList?.Add(new NotificationOption
                {
                    Content = content,
                    Title = title
                });
            });
        }

        public static void Push(NotificationOption notification)
        {
            Execute.ToUiThread(() => { NotificationList?.Add(notification); });
        }
    }
}
