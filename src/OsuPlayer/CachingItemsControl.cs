#nullable enable
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Milki.OsuPlayer;

public class CachingItemsControl : ItemsControl
{
    private readonly Stack<DependencyObject> _itemContainers = new();

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        _itemContainers.Clear();
        base.OnItemsSourceChanged(oldValue, newValue);
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return _itemContainers.Count > 0
            ? _itemContainers.Pop()
            : base.GetContainerForItemOverride();
    }

    protected override void ClearContainerForItemOverride(
        DependencyObject element, object item)
    {
        _itemContainers.Push(element);
    }
}