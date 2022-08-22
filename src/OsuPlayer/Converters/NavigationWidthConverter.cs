using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Milki.OsuPlayer.Converters;

public class NavigationWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2)
        {
            if (values[0] is bool isNavigationCollapsed && values[1] is StackPanel sp)
            {
                Storyboard sb = new Storyboard();
                DoubleAnimation da = new DoubleAnimation(isNavigationCollapsed
                    ? 48
                    : 170, TimeSpan.FromMilliseconds(300))
                {
                    From = sp.ActualWidth,
                    EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTargetProperty(da, new PropertyPath(FrameworkElement.WidthProperty));
                Storyboard.SetTarget(da, sp);
                sb.Children.Add(da);
                sb.Begin();
                //return isNavigationCollapsed
                //    ? 48
                //    : 170;
            }
        }

        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}