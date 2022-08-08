using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Milki.OsuPlayer.Shared.Models;
using Milki.OsuPlayer.UiComponents.ButtonComponent;
using Milki.OsuPlayer.Windows;

namespace Milki.OsuPlayer.Converters
{
    public class PlayingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is bool isPlaying)
            {
                if (values[1] is Window window)
                {
                    if (window is LyricWindow lyricWindow)
                    {
                        return lyricWindow.FindResource(isPlaying ? "PauseButtonTempl" : "PlayButtonTempl");
                    }
                    if (window is MainWindow mainWindow)
                    {
                        return Application.Current.FindResource(isPlaying ? "PauseButtonStyle" : "PlayButtonStyle");
                    }
                }

                if (values[1] is ContentPresenter button)
                {
                    if (button.Name == "PlayIcon")
                    {
                        return Application.Current.FindResource(isPlaying ? "WhitePauseIcon" : "WhitePlayIcon");
                    }
                }

                if (values[1] is CommonButton cb)
                {
                    return Application.Current.FindResource(isPlaying ? "PauseTempl" : "PlayTempl");
                }
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PlaylistMode playerMode)) return value;
            switch (playerMode)
            {
                case PlaylistMode.Normal:
                    return Application.Current.FindResource($"ModeNormal{parameter}Templ");
                case PlaylistMode.Random:
                    return Application.Current.FindResource($"ModeRandom{parameter}Templ");
                case PlaylistMode.Loop:
                    return Application.Current.FindResource($"ModeLoop{parameter}Templ");
                case PlaylistMode.LoopRandom:
                    return Application.Current.FindResource($"ModeLoopRandom{parameter}Templ");
                case PlaylistMode.Single:
                    return Application.Current.FindResource($"ModeSingle{parameter}Templ");
                case PlaylistMode.SingleLoop:
                    return Application.Current.FindResource($"ModeSingleLoop{parameter}Templ");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolIsFavToSvgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool b)) return Application.Current.FindResource("HeartDisabledTempl");
            return b ? Application.Current.FindResource("HeartEnabledTempl") : Application.Current.FindResource("HeartDisabledTempl");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Multi_EqualityToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0)
            {
                var first = values[0];

                foreach (var val in values.Skip(1))
                {
                    if (val != first)
                        return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Multi_ListViewSelectAndScrollConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2)
            {
                var lv = (ListView)values[0];
                var itemObj = (object)values[1];
                lv.ScrollIntoView(itemObj);
                ListViewItem item = lv.ItemContainerGenerator.ContainerFromItem(itemObj) as ListViewItem;
                item?.Focus();

                return itemObj;
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[] { value };
        }
    }
}
