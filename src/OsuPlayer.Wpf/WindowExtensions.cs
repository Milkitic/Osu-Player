using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Milki.OsuPlayer.Wpf;

public static class WindowExtensions
{
    // from winuser.h
    private const int GWL_STYLE = -16,
        WS_MAXIMIZEBOX = 0x10000,
        WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

    public static void HideMinimizeAndMaximizeButtons(this Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

        SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
    }

    public static void EnableClassicalBlur(this Window window)
    {
        var windowHelper = new WindowInteropHelper(window);

        var accent = new NativeMethods.AccentPolicy
        {
            AccentState = NativeMethods.AccentState.ACCENT_ENABLE_BLURBEHIND
        };

        var accentStructSize = Marshal.SizeOf(accent);

        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new NativeMethods.WindowCompositionAttributeData
        {
            Attribute = NativeMethods.WindowCompositionAttribute.WCA_ACCENT_POLICY,
            SizeOfData = accentStructSize,
            Data = accentPtr
        };

        _ = NativeMethods.SetWindowCompositionAttribute(windowHelper.Handle, ref data);

        Marshal.FreeHGlobal(accentPtr);
    }

    private static readonly SemaphoreSlim SingleThreadDialogSemaphore = new(1, 1);

    public static async Task ShowDialogAsync(this WindowEx window,
        CancellationToken cancellationToken = default)
    {
        await SingleThreadDialogSemaphore.WaitAsync(cancellationToken);
        var tcs = new TaskCompletionSource<object>();
        window.Closed += (sender, args) =>
        {
            SingleThreadDialogSemaphore.Release();
            tcs.TrySetResult(null);
        };

        if (cancellationToken != default)
            cancellationToken.Register(() =>
            {
                SingleThreadDialogSemaphore.Release();
                tcs.SetCanceled();
            });

        //window.Show();
        await Execute.ToUiThreadAsync(window.Show);
        if (window.IsClosed) await Task.CompletedTask;
        else await tcs.Task;
    }

    private static class NativeMethods
    {
        // ReSharper disable InconsistentNaming
        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
        // ReSharper restore InconsistentNaming

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}