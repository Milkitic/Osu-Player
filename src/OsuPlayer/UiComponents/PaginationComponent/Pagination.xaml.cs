using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Milki.OsuPlayer.Shared.Observable;
using Milki.OsuPlayer.Wpf.Command;

namespace Milki.OsuPlayer.UiComponents.PaginationComponent;

public class PaginationViewModel : VmBase
{
    private ObservableCollection<PaginationPageVm> _pages = new();
    private PaginationPageVm _lastPage;
    private PaginationPageVm _firstPage;
    private PaginationPageVm _currentPage;

    public ObservableCollection<PaginationPageVm> Pages
    {
        get => _pages;
        set => this.RaiseAndSetIfChanged(ref _pages, value);
    }

    public PaginationPageVm LastPage
    {
        get => _lastPage;
        set => this.RaiseAndSetIfChanged(ref _lastPage, value);
    }

    public PaginationPageVm FirstPage
    {
        get => _firstPage;
        set => this.RaiseAndSetIfChanged(ref _firstPage, value);
    }

    public PaginationPageVm CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }
}

/// <summary>
/// Pagination.xaml 的交互逻辑
/// </summary>
public partial class Pagination : UserControl
{
    public event Action<int> PageSelected;

    public static readonly DependencyProperty TotalCountProperty =
        DependencyProperty.Register(
            nameof(TotalCount),
            typeof(int),
            typeof(Pagination),
            new PropertyMetadata(0, OnPageChanged)
        );

    public static readonly DependencyProperty CurrentPageIndexProperty =
        DependencyProperty.Register(
            nameof(CurrentPageIndex),
            typeof(int),
            typeof(Pagination),
            new PropertyMetadata(0, OnPageChanged)
        );

    public static readonly DependencyProperty ItemsCountProperty =
        DependencyProperty.Register(
            nameof(ItemsCount),
            typeof(int),
            typeof(Pagination),
            new PropertyMetadata(100, OnPageChanged)
        );

    private readonly PaginationViewModel _viewModel;

    private static void OnPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Pagination pagination)
        {
            pagination.ResetPage();
        }
    }

    public Pagination()
    {
        InitializeComponent();
        DataContext = _viewModel = new PaginationViewModel();
    }

    /// <summary>
    /// From 0
    /// </summary>
    public int CurrentPageIndex
    {
        get => (int)GetValue(CurrentPageIndexProperty);
        set => SetValue(CurrentPageIndexProperty, value);
    }

    /// <summary>
    /// Default 0
    /// </summary>
    public int TotalCount
    {
        get => (int)GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    /// <summary>
    /// Default 0
    /// </summary>
    public int ItemsCount
    {
        get => (int)GetValue(ItemsCountProperty);
        set => SetValue(ItemsCountProperty, value);
    }

    public ICommand SwitchCommand => new DelegateCommand(obj =>
    {
        PaginationPageVm page;
        if (obj is bool isToNext)
        {
            if (_viewModel.CurrentPage == null) return;
            var nextPage = isToNext ? _viewModel.CurrentPage.Index + 1 : _viewModel.CurrentPage.Index - 1;
            page = GetActualPage(nextPage);
        }
        else
        {
            var reqPage = (int)obj;
            page = GetActualPage(reqPage);
        }

        if (page == null)
        {
            return;
        }

        if (page.IsActivated)
        {
            return;
        }

        CurrentPageIndex = page.Index - 1;
        ResetPage();
        PageSelected?.Invoke(page.Index);
    });

    private void ResetPage()
    {
        var totalPage = TotalCount;
        var currentPage = CurrentPageIndex;
        var itemsCount = ItemsCount;

        var totalPageCount = (int)Math.Ceiling(totalPage / (float)itemsCount);
        int count, startIndex;
        if (totalPageCount > 10)
        {
            if (currentPage > 5)
            {
                _viewModel.FirstPage = new PaginationPageVm(1);
                if (currentPage >= totalPageCount - 5)
                {
                    startIndex = totalPageCount - 10;
                }
                else
                {
                    startIndex = currentPage - 5;
                }
            }
            else
            {
                startIndex = 0;
            }

            count = 10;
        }
        else
        {
            count = totalPageCount;
            startIndex = 0;
        }

        _viewModel.Pages.Clear();
        for (int i = startIndex; i < startIndex + count; i++)
        {
            _viewModel.Pages.Add(new PaginationPageVm(i + 1));
        }

        var page = GetActualPage(currentPage + 1);

        if (page != null)
        {
            page.IsActivated = true;
        }

        _viewModel.CurrentPage = page;
    }

    private PaginationPageVm GetActualPage(int page)
    {
        return _viewModel.Pages.FirstOrDefault(k => k.Index == page);
    }
}