using System.Windows;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Milki.OsuPlayer.Wpf;

public static class MsgDialog
{
    public static bool Question(string content,
        string? instruction = null,
        string? title = null,
        string? footer = null,
        string? detail = null,
        IntPtr? windowHwnd = null)
    {
        try
        {
            GetWindowInfo(windowHwnd, title, out var actualHwnd, out var actualTitle);
            TaskDialog dialog = new()
            {
                InstructionText = instruction,
                Text = content,
                Caption = actualTitle,
                StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No,
            };
            if (actualHwnd != null) dialog.OwnerWindowHandle = actualHwnd.Value;
            if (footer != null) dialog.FooterText = footer;
            if (detail != null)
            {
                dialog.DetailsExpandedText = detail;
                dialog.DetailsExpanded = true;
            }
            dialog.Opened += (_, _) =>
            {
                dialog.Icon = TaskDialogStandardIcon.Information;
                if (footer != null) dialog.FooterIcon = TaskDialogStandardIcon.Information;
            };
            return dialog.Show() == TaskDialogResult.Yes;
        }
        catch (PlatformNotSupportedException)
        {
            var c = footer == null ? content : content + "\r\n\r\n" + footer;
            return MsgBox.Question(c, title);
        }
    }

    public static void Info(string content,
        string? instruction = null,
        string? title = null,
        string? footer = null,
        string? detail = null,
        IntPtr? windowHwnd = null)
    {
        try
        {
            GetWindowInfo(windowHwnd, title, out var actualHwnd, out var actualTitle);
            TaskDialog dialog = new()
            {
                InstructionText = instruction,
                Text = content,
                Caption = actualTitle,
                StandardButtons = TaskDialogStandardButtons.Ok,
            };
            if (actualHwnd != null) dialog.OwnerWindowHandle = actualHwnd.Value;
            if (footer != null) dialog.FooterText = footer;
            if (detail != null)
            {
                dialog.DetailsExpandedText = detail;
                dialog.DetailsExpanded = true;
            }
            dialog.Opened += (_, _) =>
            {
                dialog.Icon = TaskDialogStandardIcon.Information;
                if (footer != null) dialog.FooterIcon = TaskDialogStandardIcon.Information;
            };

            dialog.Show();
        }
        catch (PlatformNotSupportedException)
        {
            var c = footer == null ? content : content + "\r\n\r\n" + footer;
            MsgBox.Info(c, title);
        }
    }

    public static void Warn(string content,
        string? instruction = null,
        string? title = null,
        string? footer = null,
        string? detail = null,
        IntPtr? windowHwnd = null)
    {
        try
        {
            GetWindowInfo(windowHwnd, title, out var actualHwnd, out var actualTitle);
            TaskDialog dialog = new()
            {
                InstructionText = instruction,
                Text = content,
                Caption = actualTitle,
                StandardButtons = TaskDialogStandardButtons.Ok,
            };
            if (actualHwnd != null) dialog.OwnerWindowHandle = actualHwnd.Value;
            if (footer != null) dialog.FooterText = footer;
            if (detail != null)
            {
                dialog.DetailsExpandedText = detail;
                dialog.DetailsExpanded = true;
            }
            dialog.Opened += (_, _) =>
            {
                dialog.Icon = TaskDialogStandardIcon.Warning;
                if (footer != null) dialog.FooterIcon = TaskDialogStandardIcon.Information;
            };
            dialog.Show();
        }
        catch (PlatformNotSupportedException)
        {
            var c = footer == null ? content : content + "\r\n\r\n" + footer;
            MsgBox.Warn(c, title);
        }
    }

    public static bool WarnYesNo(string content,
        string? instruction = null,
        string? title = null,
        string? footer = null,
        string? detail = null,
        IntPtr? windowHwnd = null)
    {
        try
        {
            GetWindowInfo(windowHwnd, title, out var actualHwnd, out var actualTitle);
            TaskDialog dialog = new()
            {
                InstructionText = instruction,
                Text = content,
                Caption = actualTitle,
                StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No,
            };
            if (actualHwnd != null) dialog.OwnerWindowHandle = actualHwnd.Value;
            if (footer != null) dialog.FooterText = footer;
            if (detail != null)
            {
                dialog.DetailsExpandedText = detail;
                dialog.DetailsExpanded = true;
            }
            dialog.Opened += (_, _) =>
            {
                dialog.Icon = TaskDialogStandardIcon.Warning;
                if (footer != null) dialog.FooterIcon = TaskDialogStandardIcon.Information;
            };
            return dialog.Show() == TaskDialogResult.Yes;
        }
        catch (PlatformNotSupportedException)
        {
            var c = footer == null ? content : content + "\r\n\r\n" + footer;
            return MsgBox.WarnYesNo(c, title);
        }
    }

    public static bool WarnOkCancel(string content,
        string? instruction = null,
        string? title = null,
        string? footer = null,
        string? detail = null,
        IntPtr? windowHwnd = null)
    {
        try
        {
            GetWindowInfo(windowHwnd, title, out var actualHwnd, out var actualTitle);
            TaskDialog dialog = new()
            {
                InstructionText = instruction,
                Text = content,
                Caption = actualTitle,
                StandardButtons = TaskDialogStandardButtons.Ok | TaskDialogStandardButtons.Cancel,
            };
            if (actualHwnd != null) dialog.OwnerWindowHandle = actualHwnd.Value;
            if (footer != null) dialog.FooterText = footer;
            if (detail != null)
            {
                dialog.DetailsExpandedText = detail;
                dialog.DetailsExpanded = true;
            }
            dialog.Opened += (_, _) =>
            {
                dialog.Icon = TaskDialogStandardIcon.Warning;
                if (footer != null) dialog.FooterIcon = TaskDialogStandardIcon.Information;
            };
            return dialog.Show() == TaskDialogResult.Ok;
        }
        catch (PlatformNotSupportedException)
        {
            var c = footer == null ? content : content + "\r\n\r\n" + footer;
            return MsgBox.WarnOkCancel(c, title);
        }
    }

    public static void Error(string content,
        string? instruction = null,
        string? title = null,
        string? footer = null,
        string? detail = null,
        IntPtr? windowHwnd = null)
    {
        try
        {
            GetWindowInfo(windowHwnd, title, out var actualHwnd, out var actualTitle);
            TaskDialog dialog = new()
            {
                InstructionText = instruction,
                Text = content,
                Caption = actualTitle,
                StandardButtons = TaskDialogStandardButtons.Ok,
            };
            if (actualHwnd != null) dialog.OwnerWindowHandle = actualHwnd.Value;
            if (footer != null) dialog.FooterText = footer;
            if (detail != null)
            {
                dialog.DetailsExpandedText = detail;
                dialog.DetailsExpanded = true;
            }
            dialog.Opened += (_, _) =>
            {
                dialog.Icon = TaskDialogStandardIcon.Error;
                if (footer != null) dialog.FooterIcon = TaskDialogStandardIcon.Information;
            };
            dialog.Show();
        }
        catch (PlatformNotSupportedException)
        {
            var c = footer == null ? content : content + "\r\n\r\n" + footer;
            MsgBox.Error(c, title);
        }
    }

    private static void GetWindowInfo(IntPtr? windowHwnd, string? title, out IntPtr? actualHwnd, out string actualTitle)
    {
        actualHwnd = null;
        var window = Application.Current.MainWindow;
        if (window != null)
        {
            actualTitle = window.Title;
            actualHwnd = new WindowInteropHelper(window).Handle;
        }
        else
        {
            actualTitle = "系统提示";
        }

        if (windowHwnd != null) actualHwnd = windowHwnd;
        if (title != null) actualTitle = title;
    }
}