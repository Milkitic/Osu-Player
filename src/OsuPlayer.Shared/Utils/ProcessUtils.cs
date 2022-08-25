using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Milki.OsuPlayer.Shared.Utils;

public static class ProcessUtils
{
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    public static void StartWithShellExecute(string fileName, string? arguments = null)
    {
        Process.Start(arguments == null
            ? new ProcessStartInfo(fileName) { UseShellExecute = true }
            : new ProcessStartInfo(fileName, arguments) { UseShellExecute = true });
    }
}