using System;
using System.Windows.Controls;
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
    public partial ControlTemplate IconTemplate { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = "Title";

    [ObservableProperty]
    public partial string Content { get; set; } = "This is your content here";

    [ObservableProperty]
    public partial TimeSpan FadeoutTime { get; set; }

    [ObservableProperty]
    public partial NotificationLevel Level { get; set; }

    public string NotificationTypeString => Level.ToString();

    public bool IsEmpty => string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Content) && IconTemplate == null;

    public Action YesCallback { get; set; }
    public Action NoCallback { get; set; }
}
