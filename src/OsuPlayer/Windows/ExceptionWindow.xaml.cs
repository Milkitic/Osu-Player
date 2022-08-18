using System.Windows;
using Milki.OsuPlayer.Shared.Observable;

namespace Milki.OsuPlayer.Windows;

internal class ExceptionWindowViewModel : VmBase
{
    private Exception _exception;
    private bool _isUiException;

    public Exception Exception
    {
        get => _exception;
        set => this.RaiseAndSetIfChanged(ref _exception, value);
    }

    public bool IsUiException
    {
        get => _isUiException;
        set => this.RaiseAndSetIfChanged(ref _isUiException, value);
    }
}

/// <summary>
/// Interaction logic for ExceptionWindow.xaml
/// </summary>
public partial class ExceptionWindow : Window
{
    public ExceptionWindow(Exception ex, bool isUiException)
    {
        InitializeComponent();
        var viewModel = (ExceptionWindowViewModel)DataContext;
        viewModel.Exception = ex;
        viewModel.IsUiException = isUiException;
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnContinue_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}