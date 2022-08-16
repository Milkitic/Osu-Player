using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Milki.OsuPlayer.Wpf;

public class ExToolWindow : WindowEx
{
    [Flags]
    public enum ExtendedWindowStyles
    {
        // ...
        WS_EX_TOOLWINDOW = 0x00000080,
        // ...
    }

    public enum GetWindowLongFields
    {
        // ...
        GWL_EXSTYLE = (-20),
        // ...
    }

    private bool _forceClose;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        int error = 0;
        var result = IntPtr.Zero;
        // Win32 SetWindowLong doesn't clear error on success
        SetLastError(0);

        if (IntPtr.Size == 4)
        {
            // use SetWindowLong
            var tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
            error = Marshal.GetLastWin32Error();
            result = new IntPtr(tempResult);
        }
        else
        {
            // use SetWindowLongPtr
            result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
            error = Marshal.GetLastWin32Error();
        }

        if ((result == IntPtr.Zero) && (error != 0))
        {
            throw new System.ComponentModel.Win32Exception(error);
        }

        return result;
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

    private static int IntPtrToInt32(IntPtr intPtr)
    {
        return unchecked((int)intPtr.ToInt64());
    }

    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    private static extern void SetLastError(int dwErrorCode);

    public ExToolWindow()
    {
        ShowInTaskbar = false;
        Topmost = true;
        var app = Application.Current;
        if (app != null && app.CheckAccess())
        {
            var mainWindow = app.MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Activated += OnCurrentOnActivated;
                mainWindow.Deactivated += OnCurrentOnDeactivated;
                mainWindow.StateChanged += (sender, e) =>
                {
                    switch (mainWindow.WindowState)
                    {
                        case WindowState.Normal:
                        case WindowState.Maximized:
                            Show();
                            break;
                        case WindowState.Minimized:
                            Hide();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                };
            }

            app.Activated += OnCurrentOnActivated;
            app.Deactivated += OnCurrentOnDeactivated;
        }

        FirstLoaded += _ToolWindowBase_Loaded;
        Closing += _ToolWindowBase_Closing;

        void OnCurrentOnDeactivated(object sender, EventArgs e)
        {
            Topmost = false;
        }

        void OnCurrentOnActivated(object sender, EventArgs e)
        {
            Topmost = true;
        }
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void _ToolWindowBase_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        int exStyle = (int)GetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE);
        exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
        SetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

        if (!AllowsTransparency)
            WindowStyle = WindowStyle.ToolWindow;
    }

    private void _ToolWindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            e.Cancel = true;
        }
    }
}