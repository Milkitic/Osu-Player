#nullable enable
using System.Windows;
using System.Windows.Controls;

namespace Milki.OsuPlayer;

public class CachingItemsControl : ItemsControl
{
    private readonly Stack<DependencyObject> _itemContainers = new();

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