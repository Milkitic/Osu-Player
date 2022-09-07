#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Milki.OsuPlayer.Utils;

public class DataGridBehavior
{
    #region DisplayRowNumber

    public static readonly DependencyProperty DisplayRowNumberProperty =
        DependencyProperty.RegisterAttached("DisplayRowNumber",
            typeof(bool),
            typeof(DataGridBehavior),
            new FrameworkPropertyMetadata(false, OnDisplayRowNumberChanged));

    public static bool GetDisplayRowNumber(DependencyObject target)
    {
        return (bool)target.GetValue(DisplayRowNumberProperty);
    }

    public static void SetDisplayRowNumber(DependencyObject target, bool value)
    {
        target.SetValue(DisplayRowNumberProperty, value);
    }

    private static void OnDisplayRowNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not DataGrid dataGrid) return;
        if (e.NewValue is not true) return;

        dataGrid.LoadingRow += LoadedRowHandler;
        dataGrid.ItemContainerGenerator.ItemsChanged += ItemsChangedHandler;

        void LoadedRowHandler(object? sender, DataGridRowEventArgs args)
        {
            if (GetDisplayRowNumber(dataGrid) == false)
            {
                dataGrid.LoadingRow -= LoadedRowHandler;
                return;
            }

            args.Row.Header = $"{args.Row.GetIndex() + 1:00}";
        }

        void ItemsChangedHandler(object? sender, ItemsChangedEventArgs ea)
        {
            if (GetDisplayRowNumber(dataGrid) == false)
            {
                dataGrid.ItemContainerGenerator.ItemsChanged -= ItemsChangedHandler;
                return;
            }

            GetVisualChildCollection<DataGridRow>(dataGrid).ForEach(d => d.Header = $"{d.GetIndex() + 1:00}");
        }
    }

    #endregion // DisplayRowNumber

    #region Get Visuals

    private static List<T> GetVisualChildCollection<T>(DependencyObject parent) where T : Visual
    {
        List<T> visualCollection = new List<T>();
        GetVisualChildCollection(parent, visualCollection);
        return visualCollection;
    }

    private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T)
            {
                visualCollection.Add(child as T);
            }

            if (child != null)
            {
                GetVisualChildCollection(child, visualCollection);
            }
        }
    }

    #endregion // Get Visuals
}