using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Milki.OsuPlayer.Converters;

public class TitleVisibleConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2)
        {
            if (values[0] is bool isNavigationCollapsed && values[1] is FrameworkElement sp)
            {
                Storyboard sb = new Storyboard();

                var num = parameter == null ? 30 : 40;
                DoubleAnimation da = new DoubleAnimation(isNavigationCollapsed
                    ? 0
                    : num, TimeSpan.FromMilliseconds(300))
                {
                    From = sp.ActualHeight,
                    EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseInOut }
                };

                num = parameter == null ? 10 : System.Convert.ToInt32(parameter);

                ThicknessAnimation ta = new ThicknessAnimation(isNavigationCollapsed
                    ? new Thickness(0)
                    : new Thickness(0, num, 0, 0), TimeSpan.FromMilliseconds(300))
                {
                    From = sp.Margin,
                    EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseInOut }
                };

                Storyboard.SetTargetProperty(da, new PropertyPath(FrameworkElement.HeightProperty));
                Storyboard.SetTarget(da, sp);
                Storyboard.SetTargetProperty(ta, new PropertyPath(FrameworkElement.MarginProperty));
                Storyboard.SetTarget(ta, sp);
                sb.Children.Add(da);
                sb.Children.Add(ta);
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