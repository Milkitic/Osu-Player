namespace Milky.OsuPlayer.Services
{
    public interface IAppNotificationService
    {
        void Push(string message);

        void Push(string message, string title);
    }
}