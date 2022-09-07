using System.Collections.ObjectModel;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent;

public static class Notification
{
    public static ObservableCollection<NotificationOption> NotificationList { get; } = new();

    public static async void Push(string content, string title = null)
    {
        void InnerPush()
        {
            NotificationList?.Add(new NotificationOption { Content = content, Title = title });
        }

        await Execute.ToUiThreadAsync(InnerPush);
    }

    public static async void Push(NotificationOption notification)
    {
        void InnerPush()
        {
            NotificationList?.Add(notification);
        }

        await Execute.ToUiThreadAsync(InnerPush);
    }
}