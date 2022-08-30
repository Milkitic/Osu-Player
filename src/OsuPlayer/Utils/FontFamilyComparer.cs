using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;

namespace Milki.OsuPlayer.Utils;

public class FontFamilyComparer : IComparer<FontFamily>
{
    private readonly string _currentCulture;
    public FontFamilyComparer(CultureInfo cultureInfo)
    {
        _currentCulture = cultureInfo.Name;
    }

    public int Compare(FontFamily x, FontFamily y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;

        var xNames = x.FamilyNames;
        var yNames = y.FamilyNames;
        var xmlLanguage = XmlLanguage.GetLanguage(_currentCulture);
        if (xNames.ContainsKey(xmlLanguage) && yNames.ContainsKey(xmlLanguage))
        {
            return string.Compare(xNames[xmlLanguage], yNames[xmlLanguage], StringComparison.InvariantCulture);
        }

        if (xNames.ContainsKey(xmlLanguage))
        {
            return -1;
        }

        if (yNames.ContainsKey(xmlLanguage))
        {
            return 1;
        }

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