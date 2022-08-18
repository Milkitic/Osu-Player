using System.Collections.ObjectModel;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent;

public static class Notification
{
    public static ObservableCollection<NotificationOption> NotificationList { get; } = new();

    public static void Push(string content, string title = null)
    {
        Execute.ToUiThreadAsync(() =>
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
        Execute.ToUiThreadAsync(() => { NotificationList?.Add(notification); });
    }
}