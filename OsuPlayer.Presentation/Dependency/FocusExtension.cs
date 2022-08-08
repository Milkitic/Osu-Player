using System.Windows;

namespace Milki.OsuPlayer.Wpf.Dependency;

public static class FocusExtension
{
    public static bool GetIsFocused(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(DependencyObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }

    public static readonly DependencyProperty IsFocusedProperty =
        DependencyProperty.RegisterAttached(
            "IsFocused", typeof(bool), typeof(FocusExtension),
            new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

    private static void OnIsFocusedPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var uie = d as UIElement;
        if (e.NewValue is bool b && b)
        {
            uie?.Focus(); // Don't care about false values.
        }
    }
}