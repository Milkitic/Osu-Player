using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace Milki.OsuPlayer.Utils
{
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