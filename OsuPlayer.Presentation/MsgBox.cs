using System.Windows;

namespace Milki.OsuPlayer.Wpf;

public static class MsgBox
{
    public static bool Question(string content, string? title = null)
    {
        var result = MessageBox.Show(content,
            title ?? Application.Current?.MainWindow?.Title ?? "系统提示",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes) return true;
        return false;
    }

    public static void Info(string content, string? title = null)
    {
        MessageBox.Show(content,
            title ?? Application.Current?.MainWindow?.Title ?? "系统提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public static void Warn(string content, string? title = null)
    {
        MessageBox.Show(content,
            title ?? Application.Current?.MainWindow?.Title ?? "系统提示",
            MessageBoxButton.OK,
            MessageBoxImage.Exclamation);
    }

    public static bool WarnYesNo(string content, string? title = null)
    {
        var result = MessageBox.Show(content,
            title ?? Application.Current?.MainWindow?.Title ?? "系统提示",
            MessageBoxButton.YesNo,
            MessageBoxImage.Exclamation);
        if (result == MessageBoxResult.Yes) return true;
        return false;
    }

    public static bool WarnOkCancel(string content, string? title = null)
    {
        var result = MessageBox.Show(content,
            title ?? Application.Current?.MainWindow?.Title ?? "系统提示",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Exclamation);
        if (result == MessageBoxResult.OK) return true;
        return false;
    }

    public static void Error(string content, string? title = null)
    {
        MessageBox.Show(content,
            title ?? Application.Current?.MainWindow?.Title ?? "系统提示",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}