using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.UiComponents.NotificationComponent;

public partial class NotificationOption : ObservableObject
{
    public enum NotificationLevel
    {
        Alert,
        Confirm,
        Prompt
    }

    [ObservableProperty]
    public partial string Title { get; set; } = "Title";

    [ObservableProperty]
    public partial string Content { get; set; } = "This is your content here";

    [ObservableProperty]
    public partial NotificationLevel Level { get; set; }

    public string NotificationTypeString => Level.ToString();

    public bool IsEmpty => string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Content);
}