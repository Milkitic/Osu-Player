using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Milki.OsuPlayer.Converters;

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