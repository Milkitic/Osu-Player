using Milky.OsuPlayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Milky.OsuPlayer.Common;

namespace Milky.OsuPlayer.Control
{
    public class VirtualizingGalleryWrapPanel : VirtualizingPanel, IScrollInfo
    {
        private readonly TranslateTransform _translate = new TranslateTransform();
        private readonly HashSet<int> _loadedIndex = new HashSet<int>();

        public VirtualizingGalleryWrapPanel()
        {
            RenderTransform = _translate;
        }

        public static readonly RoutedEvent ItemLoadedEvent = EventManager.RegisterRoutedEvent(
            "ItemLoaded",
            RoutingStrategy.Bubble,
            typeof(VirtualizingGalleryRoutedEventHandler),
            typeof(VirtualizingGalleryWrapPanel));

        public event VirtualizingGalleryRoutedEventHandler ItemLoaded
        {
            add => AddHandler(ItemLoadedEvent, value);
            remove => RemoveHandler(ItemLoadedEvent, value);
        }

        public static readonly DependencyProperty ChildWidthProperty = DependencyProperty.RegisterAttached("ChildWidth",
            typeof(double),
            typeof(VirtualizingGalleryWrapPanel),
            new FrameworkPropertyMetadata(200.0,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public double ChildWidth
        {
            get => (double)GetValue(ChildWidthProperty);
            set => SetValue(ChildWidthProperty, value);
        }

        public static readonly DependencyProperty ChildHeightProperty = DependencyProperty.RegisterAttached(
            "ChildHeight",
            typeof(double),
            typeof(VirtualizingGalleryWrapPanel),
            new FrameworkPropertyMetadata(200.0,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public double ChildHeight
        {
            get => (double)GetValue(ChildHeightProperty);
            set => SetValue(ChildHeightProperty, value);
        }

        public static readonly DependencyProperty ScrollOffsetProperty = DependencyProperty.Register("ScrollOffset", typeof(int), typeof(VirtualizingGalleryWrapPanel), new PropertyMetadata(default(int)));

        public int ScrollOffset
        {
            get => (int)GetValue(ScrollOffsetProperty);
            set => SetValue(ScrollOffsetProperty, value);
        }

        public void ClearNotificationCount()
        {
            _loadedIndex.Clear();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateScrollInfo(availableSize); //availableSize更新后，更新滚动条
            // availableSize更新后，获取当前viewport内可放置的item的开始和结束索引
            // firstIndex-lastIndex之间的item可能部分在viewport中也可能都不在viewport中。
            var (firstIndex, lastIndex) = GetVisibleRange();
            // 因为配置了虚拟化，所以children的个数一直是viewport区域内的个数，
            // 如果没有虚拟化则是ItemSource的整个的个数
            if (firstIndex != _firstIndex || lastIndex != _lastIndex)
            {
                UIElementCollection children = InternalChildren;
                IItemContainerGenerator generator = ItemContainerGenerator;
                // 获得第一个可被显示的item的位置
                GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstIndex);
                int childIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1; // startPos在children中的索引

                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    int itemIndex = firstIndex;
                    while (itemIndex <= lastIndex) // 生成lastIndex-firstIndex个item
                    {
                        if (!_loadedIndex.Contains(itemIndex))
                        {
                            _loadedIndex.Add(itemIndex);
                            RaiseEvent(new VirtualizingGalleryRoutedEventArgs(ItemLoadedEvent, this) { Index = itemIndex });
                        }

                        var child = (UIElement)generator.GenerateNext(out var isNewlyRealized);
                        if (isNewlyRealized)
                        {
                            if (childIndex >= children.Count)
                                AddInternalChild(child);
                            else
                                InsertInternalChild(childIndex, child);

                            generator.PrepareItemContainer(child);

                            child.Measure(new Size(ChildWidth, ChildHeight));
                        }
                        else
                        {
                            // 处理 正在显示的child被移除了这种情况
                            if (!child.Equals(children[childIndex]))
                                RemoveInternalChildRange(childIndex, 1);
                        }


                        itemIndex++;
                        childIndex++;
                    }
                }

                CleanUpOffscreenItems(firstIndex, lastIndex);
            }

            _firstIndex = firstIndex;
            _lastIndex = lastIndex;
            return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            IItemContainerGenerator generator = ItemContainerGenerator;
            int columnCount = GetColumnCount(finalSize);
            double availableItemWidth = finalSize.Width / columnCount;

            for (int i = 0; i <= Children.Count - 1; i++)
            {
                var child = Children[i];
                int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
                int row = itemIndex / columnCount; // current row
                int column = itemIndex % columnCount;

                var x = column * availableItemWidth + (availableItemWidth - ChildWidth) / 2;
                //var x = column * ChildWidth;

                var rec = new Rect(x, row * ChildHeight, ChildWidth, ChildHeight);
                child.Arrange(rec);
            }

            return base.ArrangeOverride(finalSize);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            SetVerticalOffset(VerticalOffset);
        }

        protected override void OnClearChildren()
        {
            base.OnClearChildren();
            SetVerticalOffset(0);
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0 || index >= Children.Count)
                throw new ArgumentOutOfRangeException();
            int row = index / GetColumnCount(RenderSize);
            SetVerticalOffset(row * ChildHeight);
        }

        /// <summary>
        /// 获取所有item，在可视区域内第一个item和最后一个item的索引
        /// </summary>
        private (int firstIndex, int lastIndex) GetVisibleRange()
        {
            int childPerRow = GetColumnCount(_extent);
            var firstIndex = Convert.ToInt32(Math.Floor(_offset.Y / ChildHeight)) * childPerRow;
            var lastIndex = Convert.ToInt32(Math.Ceiling((_offset.Y + _viewPort.Height) / ChildHeight)) * childPerRow - 1;
            int itemsCount = GetItemCount(this);
            if (lastIndex >= itemsCount)
                lastIndex = itemsCount - 1;

            return (firstIndex, lastIndex);
        }

        private int GetColumnCount(Size availableSize)
        {
            var childPerRow = double.IsPositiveInfinity(availableSize.Width)
                ? Children.Count
                : Math.Max(1, Convert.ToInt32(Math.Floor(availableSize.Width / ChildWidth)));
            return childPerRow;
        }

        private int GetItemCount(DependencyObject element)
        {
            var itemsControl = ItemsControl.GetItemsOwner(element);
            return itemsControl.HasItems ? itemsControl.Items.Count : 0;
        }

        private Size GetDesiredSize(Size availableSize, int itemsCount)
        {
            int childPerRow = GetColumnCount(availableSize); // 现有宽度下 一行可以最多容纳多少个
            return new Size(childPerRow * ChildWidth,
                ChildHeight * Math.Ceiling(Convert.ToDouble(itemsCount) / childPerRow));
        }

        private int _firstIndex;
        private int _lastIndex;

        private void CleanUpOffscreenItems(int firstIndex, int lastIndex)
        {
            if (_firstIndex < firstIndex || _lastIndex > lastIndex)
            {
                //Stopwatch sw = Stopwatch.StartNew();
                if (firstIndex > _firstIndex)
                {
                    var children = InternalChildren;
                    var generator = ItemContainerGenerator;
                    //Console.WriteLine(children.Count);
                    for (int i = firstIndex - 1; i >= _firstIndex; i--)
                    {
                        var index = i - _firstIndex;
                        var childGeneratorPos = new GeneratorPosition(index, 0);
                        var sb = generator.IndexFromGeneratorPosition(childGeneratorPos);
                        if (sb == -1 || sb >= firstIndex) continue;

                        generator.Remove(childGeneratorPos, 1);
                        RemoveInternalChildRange(index, 1);
                        //Console.Write($@"{sb} ");
                    }
                }

                if (lastIndex < _lastIndex && lastIndex > -1)
                {
                    var children = InternalChildren;
                    var generator = ItemContainerGenerator;
                    //Console.WriteLine(children.Count);
                    for (int i = _lastIndex; i > lastIndex; i--)
                    {
                        var index = i - firstIndex;
                        var childGeneratorPos = new GeneratorPosition(index, 0);
                        var sb = generator.IndexFromGeneratorPosition(childGeneratorPos);
                        if (sb == -1) continue;

                        generator.Remove(childGeneratorPos, 1);
                        RemoveInternalChildRange(index, 1);
                        //Console.Write($@"{sb} ");
                    }
                }

                //Console.WriteLine();
                //Console.WriteLine(sw.ElapsedMilliseconds);
                //sw.Stop();
            }

            //if (_startIndex >= startIndex && _lastIndex <= lastIndex)
            //    return;

            //var children = InternalChildren;
            //var generator = ItemContainerGenerator;
            //int validCount = 0;
            //int invalidCount = 0;

            //for (int i = children.Count - 1; i >= 0; i--)
            //{
            //    var childGeneratorPos = new GeneratorPosition(i, 0);
            //    int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);

            //    if (itemIndex < startIndex || itemIndex > lastIndex)
            //    {
            //        generator.Remove(childGeneratorPos, 1);
            //        RemoveInternalChildRange(i, 1);
            //        validCount++;
            //    }
            //    else
            //    {
            //        invalidCount++;
            //    }
            //}

            //Console.WriteLine($@"invalid: {invalidCount}; valid: {validCount}");
        }

        #region IScrollInfo

        private void UpdateScrollInfo(Size availableSize)
        {
            var extent = GetDesiredSize(availableSize, GetItemCount(this)); // extent 自己实际需要
            if (extent != _extent)
            {
                _extent = extent;
                ScrollOwner.InvalidateScrollInfo();
            }

            if (availableSize != _viewPort)
            {
                _viewPort = availableSize;
                ScrollOwner.InvalidateScrollInfo();
            }
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - ScrollOffset);
        }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + ScrollOffset);
        }

        public void LineLeft()
        {
            //throw new NotImplementedException();
        }

        public void LineRight()
        {
            //throw new NotImplementedException();
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - _viewPort.Height);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + _viewPort.Height);
        }

        public void PageLeft()
        {
            //throw new NotImplementedException();
        }

        public void PageRight()
        {
            //throw new NotImplementedException();
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - ScrollOffset);
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + ScrollOffset);
        }

        public void MouseWheelLeft()
        {
            //throw new NotImplementedException();
        }

        public void MouseWheelRight()
        {
            //throw new NotImplementedException();
        }

        public void SetHorizontalOffset(double offset)
        {
            //throw new NotImplementedException();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || _viewPort.Height >= _extent.Height)
                offset = 0;
            else
            if (offset + _viewPort.Height >= _extent.Height)
                offset = _extent.Height - _viewPort.Height;

            _offset.Y = offset;
            ScrollOwner?.InvalidateScrollInfo();
            AnimatePosition(offset);
            InvalidateMeasure();
            // 接下来会触发MeasureOverride()
        }

        private void AnimatePosition(double offset)
        {
            var sb = new Storyboard();
            var da = new DoubleAnimation
            {
                To = -offset,
                EasingFunction = new CircleEase() { EasingMode = EasingMode.EaseOut },
                BeginTime = TimeSpan.Zero,
                Duration = Util.GetDuration(TimeSpan.FromMilliseconds(150))
            };

            Storyboard.SetTarget(da, this);
            Storyboard.SetTargetProperty(da,
                new PropertyPath("RenderTransform.Y"));
            sb.Children.Add(da);
            sb.Begin();
            //_translate.Y = -offset;
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return rectangle;
        }

        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }
        public double ExtentWidth => _extent.Width;
        public double ExtentHeight => _extent.Height;
        public double ViewportWidth => _viewPort.Width;
        public double ViewportHeight => _viewPort.Height;
        public double HorizontalOffset => _offset.X;
        public double VerticalOffset => _offset.Y;
        public ScrollViewer ScrollOwner { get; set; }

        private Size _extent = new Size(0, 0);

        private Size _viewPort = new Size(0, 0);

        private Point _offset = new Point(0, 0);

        #endregion
    }

    public class VirtualizingGalleryRoutedEventArgs : RoutedEventArgs
    {
        public VirtualizingGalleryRoutedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
        }

        public int Index { get; set; }
    }

    public delegate void VirtualizingGalleryRoutedEventHandler(object sender, VirtualizingGalleryRoutedEventArgs e);
}
