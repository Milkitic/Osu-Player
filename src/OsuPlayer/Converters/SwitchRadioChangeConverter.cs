using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Milki.OsuPlayer.UiComponents.RadioButtonComponent;

namespace Milki.OsuPlayer.Converters;

public class SwitchRadioChangeConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2)
        {
            if (values[0] is bool isNavigationCollapsed && values[1] is SwitchRadio sr)
            {
                Storyboard sb = new Storyboard();
                ThicknessAnimation da = new ThicknessAnimation(isNavigationCollapsed
                    ? new Thickness(0, 0, 150, 0)
                    : new Thickness(0, 0, 8, 0), TimeSpan.FromMilliseconds(300))
                {
                    From = sr.IconMargin,
                    EasingFunction = new QuarticEase() { EasingMode = isNavigationCollapsed ? EasingMode.EaseInOut : EasingMode.EaseOut }
                };

                Storyboard.SetTargetProperty(da, new PropertyPath(SwitchRadio.IconMarginProperty));
                Storyboard.SetTarget(da, sr);
                sb.Children.Add(da);

                var changeMargin = parameter == null;
                if (changeMargin)
                {
                    ThicknessAnimation ta = new ThicknessAnimation(isNavigationCollapsed
                        ? new Thickness(13, 0, 0, 0)
                        : new Thickness(20, 0, 0, 0), TimeSpan.FromMilliseconds(300))
                    {
                        From = sr.Padding,
                        EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseInOut }
                    };

                    Storyboard.SetTargetProperty(ta, new PropertyPath(System.Windows.Controls.Control.PaddingProperty));
                    Storyboard.SetTarget(ta, sr);
                    sb.Children.Add(ta);
                }

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