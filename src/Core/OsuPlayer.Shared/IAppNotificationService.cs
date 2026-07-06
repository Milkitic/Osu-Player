namespace OsuPlayer.Shared;

public interface IAppNotificationService
{
    void Push(string message);

    void Push(string message, string title);
}
