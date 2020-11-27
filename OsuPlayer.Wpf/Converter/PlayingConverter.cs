﻿using Milky.OsuPlayer.Windows;
using Milky.WpfApi;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Milky.OsuPlayer.Common.Player;
using System.Linq;

namespace Milky.OsuPlayer.Converter
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
                        var icon = Application.Current.FindResource(isPlaying ? "WhitePauseIcon" : "WhitePlayIcon");
                        return icon;
                    }
                }

                if (values[1] is ContentControl cc)
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
            if (!(value is PlayerMode playerMode)) return value;
            switch (playerMode)
            {
                case PlayerMode.Normal:
                    return Application.Current.FindResource($"ModeNormal{parameter}Templ");
                case PlayerMode.Random:
                    return Application.Current.FindResource($"ModeRandom{parameter}Templ");
                case PlayerMode.Loop:
                    return Application.Current.FindResource($"ModeLoop{parameter}Templ");
                case PlayerMode.LoopRandom:
                    return Application.Current.FindResource($"ModeLoopRandom{parameter}Templ");
                case PlayerMode.Single:
                    return Application.Current.FindResource($"ModeSingle{parameter}Templ");
                case PlayerMode.SingleLoop:
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
            if (!(value is bool b)) return value;
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
