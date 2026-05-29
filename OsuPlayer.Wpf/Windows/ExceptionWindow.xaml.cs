using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Milky.OsuPlayer.Windows;

internal partial class ExceptionWindowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Exception Exception { get; set; }

    [ObservableProperty]
    public partial bool IsUiException { get; set; }
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