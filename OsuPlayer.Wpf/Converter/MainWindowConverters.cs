using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Utils;

namespace Milky.OsuPlayer.Converter
{
    public class WindowMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (WindowState)value;
            return state == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //public class IconMarginConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (value is bool isNavigationCollapsed)
    //        {
    //            return isNavigationCollapsed
    //                ? new Thickness(13, 0, 0, 0)
    //                : new Thickness(20, 0, 0, 0);
    //        }

    //        return new Thickness(0);
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class TitleIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isNavigationCollapsed)
            {
                return !isNavigationCollapsed
                    ? App.Current.FindResource("TitleLogo")
                    : App.Current.FindResource("TitleLogoSmall");
            }

            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
                        : num, new Duration(TimeSpan.FromMilliseconds(300)))
                    {
                        From = sp.ActualHeight,
                        EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseInOut }
                    };

                     num = parameter == null ? 10 : System.Convert.ToInt32(parameter);

                    ThicknessAnimation ta = new ThicknessAnimation(isNavigationCollapsed
                        ? new Thickness(0)
                        : new Thickness(0, num, 0, 0), new Duration(TimeSpan.FromMilliseconds(300)))
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
                        : 170, new Duration(TimeSpan.FromMilliseconds(300)))
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
                        : new Thickness(0, 0, 8, 0), new Duration(TimeSpan.FromMilliseconds(300)))
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
                            : new Thickness(20, 0, 0, 0), new Duration(TimeSpan.FromMilliseconds(300)))
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
    public class MiniEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LyricWindowShownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var shown = (bool)value;
            if (System.Convert.ToString(parameter) == "string")
            {
                return shown ? I18nUtil.GetString("ui-closeDesktopLyric") : I18nUtil.GetString("ui-openDesktopLyric");
            }
            else if (System.Convert.ToString(parameter) == "bool")
            {
                return shown;
            }
            else return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LyricWindowLockedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var locked = (bool)value;
            return locked ? I18nUtil.GetString("ui-unlockLyric") : I18nUtil.GetString("ui-lockLyric");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolTrueToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var show = (bool)value;
            return show ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
