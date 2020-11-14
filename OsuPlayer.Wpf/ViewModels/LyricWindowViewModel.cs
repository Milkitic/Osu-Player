using System;
using Milky.OsuPlayer.Common;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Media.Audio;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Shared.Dependency;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace Milky.OsuPlayer.ViewModels
{
    public class LyricWindowViewModel : VmBase
    {
        private bool _showFrame;
        private bool _isLyricEnabled;
        private object _fontFamily;
        private double _hue;
        private double _saturation;
        private double _lightness;

        public ObservablePlayController Controller { get; } = Service.Get<ObservablePlayController>();
        public SharedVm Shared { get; } = SharedVm.Default;

        public bool ShowFrame
        {
            get => _showFrame;
            set
            {
                _showFrame = value;
                OnPropertyChanged();
            }
        }


        public bool IsLyricWindowShown
        {
            get => _isLyricEnabled;
            set
            {
                _isLyricEnabled = value;
                OnPropertyChanged();
            }
        }

        public object FontFamily
        {
            get => _fontFamily;
            set
            {
                if (Equals(value, _fontFamily)) return;
                _fontFamily = value;
                AppSettings.Default.Lyric.FontFamily = _fontFamily?.ToString();
                AppSettings.SaveDefault();
                OnPropertyChanged();
            }
        }

        public ICollection<FontFamily> AllFontFamilies { get; } = new SortedSet<FontFamily>(Fonts.SystemFontFamilies.Concat(new[]
            {(FontFamily) Application.Current.FindResource("SspRegular")}), new FontFamilyComparer());

        public double Hue
        {
            get => _hue;
            set
            {
                if (value.Equals(_hue)) return;
                _hue = value;
                OnPropertyChanged();
            }
        }

        public double Saturation
        {
            get => _saturation;
            set
            {
                if (value.Equals(_saturation)) return;
                _saturation = value;
                OnPropertyChanged();
            }
        }

        public double Lightness
        {
            get => _lightness;
            set
            {
                if (value.Equals(_lightness)) return;
                _lightness = value;
                OnPropertyChanged();
            }
        }
    }

    public class FontFamilyComparer : IComparer<FontFamily>
    {
        private string _currentCulture = CultureInfo.CurrentUICulture.Name;
        public int Compare(FontFamily x, FontFamily y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            var xNames = x.FamilyNames;
            var yNames = y.FamilyNames;
            if (xNames.ContainsKey(XmlLanguage.GetLanguage(_currentCulture)) &&
                yNames.ContainsKey(XmlLanguage.GetLanguage(_currentCulture)))
            {
                var kvpX = xNames[XmlLanguage.GetLanguage(_currentCulture)];
                var kvpY = yNames[XmlLanguage.GetLanguage(_currentCulture)];

                return string.Compare(kvpX, kvpY, StringComparison.InvariantCulture);
            }
            else if (xNames.ContainsKey(XmlLanguage.GetLanguage(_currentCulture)))
            {
                return -1;
            }
            else if (yNames.ContainsKey(XmlLanguage.GetLanguage(_currentCulture)))
            {
                return 1;
            }
            else
            {
                var kvpX = xNames.FirstOrDefault(k =>
                    k.Key.IetfLanguageTag != "en-us");
                var kvpY = yNames.FirstOrDefault(k =>
                    k.Key.IetfLanguageTag != "en-us");
                if (kvpX.Key == null) kvpX = xNames.FirstOrDefault();
                if (kvpY.Key == null) kvpY = yNames.FirstOrDefault();

                if (kvpX.Key != kvpY.Key)
                {
                    if (kvpX.Key.IetfLanguageTag == "en-us") return 1;
                    if (kvpY.Key.IetfLanguageTag == "en-us") return -1;

                    return string.Compare(kvpX.Key.IetfLanguageTag, kvpY.Key.IetfLanguageTag, StringComparison.Ordinal);
                }

                return string.Compare(kvpX.Value, kvpY.Value, StringComparison.InvariantCulture);
            }
        }
    }
}