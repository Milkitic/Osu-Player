using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Anotar.NLog;

namespace Milki.OsuPlayer.UiComponents.NotificationComponent;

/// <summary>
/// NotifyControl.xaml 的交互逻辑
/// </summary>
public partial class NotifyControl : UserControl
{
    private readonly ObservableCollection<NotificationOption> _baseCollection;
    private readonly Timer _timer;
    private bool _hidden;

    public NotificationOption ViewModel { get; }

    public NotifyControl(NotificationOption notificationOption, ObservableCollection<NotificationOption> baseCollection)
    {
        _baseCollection = baseCollection;
        InitializeComponent();
        DataContext = notificationOption;
        ViewModel = notificationOption;

        if (notificationOption.IsEmpty)
        {
            _baseCollection.Remove(notificationOption);
            this.Visibility = Visibility.Collapsed;
            return;
        }

        switch (notificationOption.NotificationLevel)
        {
            case NotificationLevel.Normal:
                BoxFlag.Fill = Brushes.Transparent;
                break;
            case NotificationLevel.Warn:
                BoxFlag.Fill = (Brush)FindResource("OrangeBrush");
                break;
            case NotificationLevel.Error:
                BoxFlag.Fill = (Brush)FindResource("RedBrush");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (notificationOption.NotificationType == NotificationType.Alert &&
            notificationOption.FadeoutTime > TimeSpan.FromSeconds(1))
        {
            _timer = new Timer(obj =>
            {
                try
                {
                    Dispatcher.Invoke(TriggerHide);
                }
                catch (Exception e)
                {
                    LogTo.Error("HideTrigger Error: " + e.Message);
                }
                _timer?.Dispose();
            }, null, (int)notificationOption.FadeoutTime.TotalMilliseconds, Timeout.Infinite);
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        //return;
        TriggerShow();
    }

    private void NotifyControl_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.NotificationType != NotificationType.Alert)
        {
            return;
        }

        TriggerHide();
    }

    #region Animation

    private void TriggerShow()
    {
        var height = NotifyBorder.ActualHeight;
        var easing = new CircleEase
        {
            EasingMode = EasingMode.EaseOut,
        };
        var timing = TimeSpan.FromMilliseconds(200);

        var vector = new DoubleAnimation
        {
            From = 0,
            To = height,
            EasingFunction = easing,
            Duration = new Duration(timing)
        };

        Storyboard.SetTargetName(vector, NotifyBorder.Name);
        Storyboard.SetTargetProperty(vector,
            new PropertyPath(HeightProperty));

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 0,
            EasingFunction = easing,
            Duration = new Duration(timing)
        };
        Storyboard.SetTargetName(fade, NotifyBorder.Name);
        Storyboard.SetTargetProperty(fade,
            new PropertyPath(OpacityProperty));

        var sb = new Storyboard();
        sb.Children.Add(vector);
        sb.Children.Add(fade);
        sb.Completed += ShowThisNextAction;
        sb.Begin(this);
    }

    private void ShowThisNextAction(object sender, EventArgs e)
    {
        var width = NotifyBorder.ActualWidth;
        var easing = new CircleEase
        {
            EasingMode = EasingMode.EaseOut,
        };
        var timing = TimeSpan.FromMilliseconds(300);
        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            EasingFunction = easing,
            Duration = new Duration(timing)
        };
        Storyboard.SetTargetName(fade, NotifyBorder.Name);
        Storyboard.SetTargetProperty(fade,
            new PropertyPath(OpacityProperty));

        var vector = new ThicknessAnimation
        {
            From = new Thickness(width, 0, -width, 0),
            To = new Thickness(0),
            EasingFunction = easing,
            Duration = new Duration(timing)
        };
        Storyboard.SetTargetName(vector, NotifyBorder.Name);
        Storyboard.SetTargetProperty(vector,
            new PropertyPath(MarginProperty));

        var sb = new Storyboard();
        sb.Children.Add(fade);
        sb.Children.Add(vector);
        sb.Begin(this);
    }

    private void TriggerHide()
    {
        if (_hidden) return;
        _hidden = true;

        var width = NotifyBorder.ActualWidth;
        var easing = new CubicEase
        {
            EasingMode = EasingMode.EaseOut,
        };
        var timing = TimeSpan.FromMilliseconds(300);
        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            EasingFunction = easing,
            Duration = new Duration(timing)
        };
        Storyboard.SetTargetName(fade, NotifyBorder.Name);
        Storyboard.SetTargetProperty(fade,
            new PropertyPath(OpacityProperty));

        var vector = new ThicknessAnimation
        {
            From = new Thickness(0),
            To = new Thickness(width, 0, -width, 0),
            EasingFunction = easing,
            Duration = new Duration(timing)
        };
        Storyboard.SetTargetName(vector, NotifyBorder.Name);
        Storyboard.SetTargetProperty(vector,
            new PropertyPath(MarginProperty));

        var sb = new Storyboard();
        sb.Children.Add(fade);
        sb.Children.Add(vector);
        sb.Completed += HideThisNextAction;
        sb.Begin(this);
        _baseCollection.Remove(this.ViewModel);
    }

    private void HideThisNextAction(object sender, EventArgs e)
    {
        var height = NotifyBorder.ActualHeight;
        var easing = new CircleEase
        {
            EasingMode = EasingMode.EaseOut,
        };
        var timing = TimeSpan.FromMilliseconds(200);

        var vector = new DoubleAnimation
        {
            From = height,
            To = 0,
            EasingFunction = easing,
            Duration = new Duration(timing)
        };

        Storyboard.SetTargetName(vector, NotifyBorder.Name);
        Storyboard.SetTargetProperty(vector,
            new PropertyPath(HeightProperty));
        var fade = new DoubleAnimation
        {
            From = 0,
            To = 0,
            EasingFunction = easing,
            Duration = new Duration(timing)
        };
        Storyboard.SetTargetName(fade, NotifyBorder.Name);
        Storyboard.SetTargetProperty(fade,
            new PropertyPath(OpacityProperty));

        var sb = new Storyboard();
        sb.Children.Add(vector);
        sb.Children.Add(fade);
        sb.Begin(this);
    }

    #endregion

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.YesCallback?.Invoke();
        TriggerHide();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NoCallback?.Invoke();
        TriggerHide();
    }
}