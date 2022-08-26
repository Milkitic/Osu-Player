using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Anotar.NLog;
using Milki.OsuPlayer.Data.Models;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.UserControls;

public class CardCollectionControlVm : VmBase
{
    private HashSet<IDisplayablePlayItem> _existsObjHashSet = new();

    private ObservableCollection<IDisplayablePlayItem> _visiblePlayItems = new();
    private double _canvasWidth;
    private double _canvasHeight;

    public double ItemMargin { get; set; } = 3;

    public double ViewportWidth { get; set; }
    public double ViewportHeight { get; set; }
    public double CardWidth { get; set; }
    public double CardHeight { get; set; }

    public double ScrollViewerVerticalOffset { get; set; }
    
    public HashSet<IDisplayablePlayItem> LoadedObjHashSet { get; } = new();
    public IDisplayablePlayItem[] FullPlayItems { get; set; } = Array.Empty<IDisplayablePlayItem>();

    public ObservableCollection<IDisplayablePlayItem> VisiblePlayItems
    {
        get => _visiblePlayItems;
        set => this.RaiseAndSetIfChanged(ref _visiblePlayItems, value);
    }

    public double CanvasWidth
    {
        get => _canvasWidth;
        set => this.RaiseAndSetIfChanged(ref _canvasWidth, value);
    }

    public double CanvasHeight
    {
        get => _canvasHeight;
        set => this.RaiseAndSetIfChanged(ref _canvasHeight, value);
    }

    public void SetVisibleObjects()
    {
        if (FullPlayItems is not { Length: > 0 }) return;

        var itemMargin = ItemMargin;
        var viewportWidth = ViewportWidth;
        var cardWidth = CardWidth;

        var canvasHeight = CanvasHeight;
        var canvasWidth = cardWidth;

        var cols = (int)((viewportWidth - itemMargin * 2) / (cardWidth + itemMargin * 2));
        var rows = (int)Math.Ceiling(FullPlayItems.Length / (double)cols);

        var startOffset = ScrollViewerVerticalOffset;
        var endOffset = ScrollViewerVerticalOffset + ViewportHeight;

        var startRow = (int)Math.Floor(startOffset / (canvasHeight + 6) * rows);
        var endRow = (int)Math.Ceiling(endOffset / (canvasHeight + 6) * rows);

        var startIndex = startRow * cols;
        var count = (endRow - startRow + 1) * cols;
        var visibleItems = new HashSet<IDisplayablePlayItem>(count);

        for (int i = startIndex; i < count + startIndex; i++)
        {
            if (i >= FullPlayItems.Length) break;
            var displayablePlayItem = FullPlayItems[i];
            visibleItems.Add(displayablePlayItem);
            displayablePlayItem.CanvasIndex = FullPlayItems.Length - 1 - i;

            var row = i / cols;
            var col = i % cols;
            displayablePlayItem.CanvasLeft = itemMargin + (itemMargin * 2 + cardWidth) * col + itemMargin;
            displayablePlayItem.CanvasTop = -itemMargin + (itemMargin * 2 + CardHeight) * row + itemMargin;
        }

        var newObjs = visibleItems.Where(k => !_existsObjHashSet.Contains(k));
        var existObjs = visibleItems.Where(k => _existsObjHashSet.Contains(k));
        var notExistsAnyMore = _existsObjHashSet.Except(existObjs.Concat(newObjs));
        foreach (var displayablePlayItem in notExistsAnyMore)
        {
            VisiblePlayItems.Remove(displayablePlayItem);
        }

        foreach (var displayablePlayItem in newObjs)
        {
            VisiblePlayItems.Add(displayablePlayItem);
            if (LoadedObjHashSet.Add(displayablePlayItem))
            {
                DelayLoadPlayItem(displayablePlayItem);
            }
        }

        _existsObjHashSet = visibleItems;
    }

    private static async void DelayLoadPlayItem(IDisplayablePlayItem displayablePlayItem)
    {
        var playItem = displayablePlayItem.CurrentPlayItem;
        try
        {
            var fileName = await CommonUtils.GetThumbByBeatmapDbId(playItem);
            //LogTo.Debug("Card collection thumb loaded: " + fileName);
            playItem.PlayItemAsset!.FullThumbPath = fileName;
            displayablePlayItem.ThumbPath = fileName;
        }
        catch (Exception ex)
        {
            LogTo.ErrorException("Error while loading panel item.", ex);
        }
    }
}

/// <summary>
/// CardCollectionControl.xaml 的交互逻辑
/// </summary>
public partial class CardCollectionControl : UserControl
{
    public static readonly DependencyProperty PlayItemsProperty = DependencyProperty.Register(
        nameof(PlayItems), typeof(IEnumerable<IDisplayablePlayItem>), typeof(CardCollectionControl), new PropertyMetadata(default(IEnumerable<IDisplayablePlayItem>), CollectionChanged));

    private static void CollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CardCollectionControl cardCollectionControl &&
            e.NewValue is IEnumerable<IDisplayablePlayItem> collection)
        {
            cardCollectionControl.SetCollection(collection);
        }
    }

    public static readonly DependencyProperty CardWidthProperty = DependencyProperty.Register(
        nameof(CardWidth), typeof(double), typeof(CardCollectionControl), new PropertyMetadata(default(double), CardSizeChanged));
    public static readonly DependencyProperty CardHeightProperty = DependencyProperty.Register(
        nameof(CardHeight), typeof(double), typeof(CardCollectionControl), new PropertyMetadata(default(double), CardSizeChanged));

    private static void CardSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CardCollectionControl cardCollectionControl &&
            e.NewValue is double)
        {
            cardCollectionControl.SetCardSize();
        }
    }

    private readonly CardCollectionControlVm _viewModel;
    private bool _firstLoaded;

    public CardCollectionControl()
    {
        DataContext = _viewModel = new CardCollectionControlVm();
        InitializeComponent();

        if (PlayItems != null)
        {
            SetCollection(PlayItems);
        }
    }

    public IEnumerable<IDisplayablePlayItem> PlayItems
    {
        get => (IEnumerable<IDisplayablePlayItem>)GetValue(PlayItemsProperty);
        set => SetValue(PlayItemsProperty, value);
    }

    public double CardWidth
    {
        get => (double)GetValue(CardWidthProperty);
        set => SetValue(CardWidthProperty, value);
    }

    public double CardHeight
    {
        get => (double)GetValue(CardHeightProperty);
        set => SetValue(CardHeightProperty, value);
    }

    private void SetCollection(IEnumerable<IDisplayablePlayItem> playItems)
    {
        ScrollViewer.ScrollToVerticalOffset(0);
        _viewModel.FullPlayItems = playItems as IDisplayablePlayItem[] ??
                                    playItems.ToArray();
        _viewModel.LoadedObjHashSet.Clear();
        SetCanvasSize();
        _viewModel.SetVisibleObjects();
    }

    private void SetCardSize()
    {
        _viewModel.CardWidth = CardWidth;
        _viewModel.CardHeight = CardHeight;
        SetCanvasSize();
        _viewModel.SetVisibleObjects();
    }

    private void SetViewportSize()
    {
        _viewModel.ViewportWidth = ScrollViewer.ViewportWidth;
        _viewModel.ViewportHeight = ScrollViewer.ViewportHeight;
        SetCanvasSize();
        _viewModel.SetVisibleObjects();
    }

    private void SetVerticalOffset()
    {
        _viewModel.ScrollViewerVerticalOffset = ScrollViewer.VerticalOffset;
        _viewModel.SetVisibleObjects();
    }

    private void SetCanvasSize()
    {
        if (_viewModel.ViewportWidth == 0 || _viewModel.FullPlayItems.Length == 0)
        {
            _viewModel.CanvasWidth = _viewModel.ViewportWidth;
            _viewModel.CanvasHeight = 0;
            return;
        }

        var itemMargin = _viewModel.ItemMargin;
        var viewportWidth = _viewModel.ViewportWidth;
        var viewportHeight = _viewModel.ViewportHeight;
        var cardWidth = _viewModel.CardWidth;

        var cols = (int)((viewportWidth - itemMargin * 2) / (cardWidth + itemMargin * 2));
        var rows = (int)Math.Ceiling(_viewModel.FullPlayItems.Length / (double)cols);

        _viewModel.CanvasWidth = viewportWidth;
        _viewModel.CanvasHeight = Math.Max(rows * (_viewModel.CardHeight + itemMargin * 2) - itemMargin * 2, viewportHeight);
    }

    private void FrameworkElement_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetViewportSize();
    }

    private void CardCollectionControl_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_firstLoaded) return;
        _firstLoaded = true;
        SetViewportSize();
        SetCanvasSize();
    }

    private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        SetVerticalOffset();
    }
}