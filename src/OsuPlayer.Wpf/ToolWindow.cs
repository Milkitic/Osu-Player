using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Milki.OsuPlayer.Wpf;

public abstract class ToolWindow : WindowEx
{
    private bool _forceClose;

    private const int GWL_STYLE = -16;
    private const int WS_SYSMENU = 0x80000;
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_LAYERED = 0x80000;
    public const int LWA_ALPHA = 0x2;
    public const int LWA_COLORKEY = 0x1;

    protected ToolWindow()
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
        ContentRendered += _ToolWindowBase_ContentRendered;

        void OnCurrentOnDeactivated(object sender, EventArgs e)
        {
            Topmost = false;
        }

        void OnCurrentOnActivated(object sender, EventArgs e)
        {
            Topmost = true;
        }
    }

    private void _ToolWindowBase_ContentRendered(object sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;

        SetWindowLong(handle, GWL_EXSTYLE, GetWindowLong(handle, GWL_EXSTYLE) ^ WS_EX_LAYERED);
        SetLayeredWindowAttributes(handle, 0, 240, LWA_ALPHA);
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void _ToolWindowBase_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

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