using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace OsuPlayer.Controls.PanelComponent;

public class VirtualizingGalleryWrapPanel : VirtualizingPanel
{
    private const int FallbackViewportRows = 3;
    private readonly Dictionary<int, RealizedItem> _realizedItems = new();
    private readonly HashSet<int> _loadedIndexes = new();
    private Size _extent;
    private Rect _viewport;

    static VirtualizingGalleryWrapPanel()
    {
        AffectsMeasure<VirtualizingGalleryWrapPanel>(
            ChildWidthProperty,
            ChildHeightProperty,
            ScrollOffsetProperty);
    }

    public event EventHandler<ItemLoadedEventArgs>? ItemLoaded;

    public static readonly StyledProperty<double> ChildWidthProperty =
        AvaloniaProperty.Register<VirtualizingGalleryWrapPanel, double>(nameof(ChildWidth), 200);

    public double ChildWidth
    {
        get => GetValue(ChildWidthProperty);
        set => SetValue(ChildWidthProperty, value);
    }

    public static readonly StyledProperty<double> ChildHeightProperty =
        AvaloniaProperty.Register<VirtualizingGalleryWrapPanel, double>(nameof(ChildHeight), 200);

    public double ChildHeight
    {
        get => GetValue(ChildHeightProperty);
        set => SetValue(ChildHeightProperty, value);
    }

    public static readonly StyledProperty<double> ScrollOffsetProperty =
        AvaloniaProperty.Register<VirtualizingGalleryWrapPanel, double>(nameof(ScrollOffset), 100);

    public double ScrollOffset
    {
        get => GetValue(ScrollOffsetProperty);
        set => SetValue(ScrollOffsetProperty, value);
    }

    public void ClearNotificationCount()
    {
        _loadedIndexes.Clear();
    }

    protected override Control? ScrollIntoView(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return null;
        }

        ScrollIndexIntoView(index);
        return ContainerFromIndex(index);
    }

    protected override Control? ContainerFromIndex(int index)
    {
        return _realizedItems.TryGetValue(index, out var item) ? item.Container : null;
    }

    protected override int IndexFromContainer(Control container)
    {
        foreach (var item in _realizedItems)
        {
            if (ReferenceEquals(item.Value.Container, container))
            {
                return item.Key;
            }
        }

        return -1;
    }

    protected override IEnumerable<Control>? GetRealizedContainers()
    {
        foreach (var item in _realizedItems.Values)
        {
            yield return item.Container;
        }
    }

    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(items, e);
        ClearNotificationCount();
        ClearRealizedItems();
        _viewport = default;
        _extent = default;
        ResetScrollOffset();
        InvalidateMeasure();
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var fromIndex = from is Control control ? IndexFromContainer(control) : -1;
        var columns = GetColumnCount(_viewport.Width);
        var targetIndex = direction switch
        {
            NavigationDirection.First => 0,
            NavigationDirection.Last => Items.Count - 1,
            NavigationDirection.Left => fromIndex - 1,
            NavigationDirection.Right => fromIndex + 1,
            NavigationDirection.Up => fromIndex - columns,
            NavigationDirection.Down => fromIndex + columns,
            _ => -1
        };

        if (wrap && Items.Count > 0)
        {
            if (targetIndex < 0)
            {
                targetIndex = Items.Count - 1;
            }
            else if (targetIndex >= Items.Count)
            {
                targetIndex = 0;
            }
        }

        if (targetIndex < 0 || targetIndex >= Items.Count)
        {
            return null;
        }

        return ScrollIntoView(targetIndex);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = GetAvailableWidth(availableSize);
        var extent = UpdateExtent(width);
        EnsureViewport(availableSize, extent);
        CoerceViewport(width, availableSize, extent);

        var (firstIndex, lastIndex) = GetVisibleRange();
        RealizeRange(firstIndex, lastIndex);
        CleanUpOffscreenItems(firstIndex, lastIndex);

        foreach (var item in _realizedItems.Values)
        {
            item.Container.Measure(new Size(ChildWidth, ChildHeight));
        }

        return extent;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = GetColumnCount(finalSize.Width);
        var availableItemWidth = finalSize.Width / columns;

        foreach (var item in _realizedItems)
        {
            var row = item.Key / columns;
            var column = item.Key % columns;
            var x = column * availableItemWidth + Math.Max(0, (availableItemWidth - ChildWidth) / 2);
            var y = row * ChildHeight;
            item.Value.Container.Arrange(new Rect(x, y, ChildWidth, ChildHeight));
        }

        return finalSize;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        EffectiveViewportChanged -= OnEffectiveViewportChanged;
        ClearRealizedItems();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        var viewport = e.EffectiveViewport;
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            viewport = viewport.Intersect(new Rect(Bounds.Size));
        }

        if (!IsUsableRect(viewport) || viewport == _viewport)
        {
            return;
        }

        _viewport = viewport;
        InvalidateMeasure();
    }

    private void RealizeRange(int firstIndex, int lastIndex)
    {
        var generator = ItemContainerGenerator;
        if (firstIndex > lastIndex || generator == null)
        {
            return;
        }

        for (var index = firstIndex; index <= lastIndex; index++)
        {
            if (_realizedItems.ContainsKey(index))
            {
                continue;
            }

            var item = Items[index];
            var needsContainer = generator.NeedsContainer(item, index, out var recycleKey);
            var container = needsContainer
                ? generator.CreateContainer(item, index, recycleKey)
                : item as Control;
            if (container == null)
            {
                continue;
            }

            if (needsContainer)
            {
                generator.PrepareItemContainer(container, item, index);
            }

            AddInternalChild(container);
            _realizedItems.Add(index, new RealizedItem(container, needsContainer));
            generator.ItemContainerPrepared(container, item, index);

            if (_loadedIndexes.Add(index))
            {
                ItemLoaded?.Invoke(this, new ItemLoadedEventArgs(index));
            }
        }
    }

    private void CleanUpOffscreenItems(int firstIndex, int lastIndex)
    {
        if (_realizedItems.Count == 0)
        {
            return;
        }

        var toRemove = new List<int>();
        foreach (var index in _realizedItems.Keys)
        {
            if (index < firstIndex || index > lastIndex)
            {
                toRemove.Add(index);
            }
        }

        foreach (var index in toRemove)
        {
            Unrealize(index);
        }
    }

    private void ClearRealizedItems()
    {
        if (_realizedItems.Count == 0)
        {
            return;
        }

        var indexes = new List<int>(_realizedItems.Keys);
        foreach (var index in indexes)
        {
            Unrealize(index);
        }
    }

    private void Unrealize(int index)
    {
        if (!_realizedItems.Remove(index, out var item))
        {
            return;
        }

        if (item.NeedsContainer && ItemContainerGenerator != null)
        {
            ItemContainerGenerator.ClearItemContainer(item.Container);
        }

        RemoveInternalChild(item.Container);
    }

    private (int firstIndex, int lastIndex) GetVisibleRange()
    {
        if (Items.Count <= 0)
        {
            return (0, -1);
        }

        var columns = GetColumnCount(_viewport.Width);
        var firstRow = Math.Max(0, (int)Math.Floor(_viewport.Top / ChildHeight) - 1);
        var lastRow = Math.Max(firstRow, (int)Math.Ceiling(_viewport.Bottom / ChildHeight) + 1);
        var firstIndex = firstRow * columns;
        var lastIndex = Math.Min(Items.Count - 1, ((lastRow + 1) * columns) - 1);

        return (firstIndex, lastIndex);
    }

    private Size UpdateExtent(double availableWidth)
    {
        var columns = GetColumnCount(availableWidth);
        var rows = Items.Count == 0 ? 0 : (int)Math.Ceiling(Items.Count / (double)columns);
        var extent = new Size(Math.Max(availableWidth, columns * ChildWidth), rows * ChildHeight);

        if (_extent != extent)
        {
            _extent = extent;
        }

        return extent;
    }

    private void ScrollIndexIntoView(int index)
    {
        var columns = GetColumnCount(_viewport.Width);
        var row = index / columns;
        SetScrollOffset(row * ChildHeight);
    }

    private void SetScrollOffset(double verticalOffset)
    {
        var scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer == null)
        {
            return;
        }

        var maxY = Math.Max(0, _extent.Height - _viewport.Height);
        var y = Math.Clamp(verticalOffset, 0, maxY);
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, y);
        InvalidateMeasure();
    }

    private void ResetScrollOffset()
    {
        SetScrollOffset(0);
    }

    private int GetColumnCount(double availableWidth)
    {
        if (double.IsInfinity(availableWidth) || availableWidth <= 0 || double.IsNaN(availableWidth))
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Floor(availableWidth / ChildWidth));
    }

    private Size GetViewportSize(Size availableSize)
    {
        var width = GetAvailableWidth(availableSize);
        var height = GetAvailableHeight(availableSize);

        return new Size(width, height);
    }

    private void EnsureViewport(Size availableSize, Size extent)
    {
        if (IsUsableRect(_viewport))
        {
            return;
        }

        var viewportSize = GetViewportSize(availableSize);
        viewportSize = new Size(
            Math.Min(viewportSize.Width, extent.Width),
            Math.Min(viewportSize.Height, extent.Height));
        _viewport = new Rect(viewportSize);
    }

    private void CoerceViewport(double availableWidth, Size availableSize, Size extent)
    {
        if (!IsUsableRect(_viewport))
        {
            return;
        }

        var width = Math.Min(availableWidth, extent.Width);
        var height = _viewport.Height;
        if (!double.IsInfinity(availableSize.Height) && availableSize.Height > 0 && !double.IsNaN(availableSize.Height))
        {
            height = availableSize.Height;
        }

        height = Math.Min(height, extent.Height);
        var maxY = Math.Max(0, extent.Height - height);
        var y = Math.Clamp(_viewport.Y, 0, maxY);
        var nextViewport = new Rect(0, y, width, height);
        if (nextViewport != _viewport)
        {
            _viewport = nextViewport;
        }
    }

    private double GetAvailableWidth(Size availableSize)
    {
        if (!double.IsInfinity(availableSize.Width) && availableSize.Width > 0)
        {
            return availableSize.Width;
        }

        if (ItemsControl?.Bounds.Width is > 0 and var ownerWidth)
        {
            return ownerWidth;
        }

        return Bounds.Width > 0 ? Bounds.Width : ChildWidth;
    }

    private double GetAvailableHeight(Size availableSize)
    {
        if (!double.IsInfinity(availableSize.Height) && availableSize.Height > 0 && !double.IsNaN(availableSize.Height))
        {
            return availableSize.Height;
        }

        if (ItemsControl?.Bounds.Height is > 0 and var ownerHeight)
        {
            return ownerHeight;
        }

        if (Bounds.Height > 0)
        {
            return Bounds.Height;
        }

        return ChildHeight * FallbackViewportRows;
    }

    private static bool IsUsableRect(Rect rect)
    {
        return rect.Width > 0 &&
               rect.Height > 0 &&
               !double.IsNaN(rect.Width) &&
               !double.IsNaN(rect.Height) &&
               !double.IsInfinity(rect.Width) &&
               !double.IsInfinity(rect.Height);
    }

    private readonly record struct RealizedItem(Control Container, bool NeedsContainer);
}

public sealed class ItemLoadedEventArgs : EventArgs
{
    public ItemLoadedEventArgs(int index)
    {
        Index = index;
    }

    public int Index { get; }
}
