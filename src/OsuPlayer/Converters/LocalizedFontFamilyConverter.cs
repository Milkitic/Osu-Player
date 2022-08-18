using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Milki.OsuPlayer.Converters
{
    public class LocalizedFontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FontFamily fontFamily)
            {
                var languageSpecificStringDictionary = fontFamily.FamilyNames;
                if (languageSpecificStringDictionary.TryGetValue(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.Name), out var fontName))
                {
                    return fontName;
                }
                else if (languageSpecificStringDictionary.Count > 1)
                {
                    var name = languageSpecificStringDictionary.FirstOrDefault(k =>
                        k.Key != XmlLanguage.GetLanguage("en-us")).Value;
                    return name;
                }
                else
                {
                    return languageSpecificStringDictionary.FirstOrDefault().Value;
                }
            }

            throw new ArgumentNullException(nameof(fontFamily));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}