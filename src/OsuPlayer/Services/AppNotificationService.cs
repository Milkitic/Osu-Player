using OsuPlayer.UiComponents.NotificationComponent;

namespace OsuPlayer.Services
{
    public sealed class AppNotificationService : IAppNotificationService
    {
        public void Push(string message)
        {
            Notification.Push(message);
        }

        public void Push(string message, string title)
        {
            Notification.Push(message, title);
        }
    }
}