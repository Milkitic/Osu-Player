using System;
using System.Runtime.InteropServices;

namespace Milki.OsuPlayer.Wpf;

public static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPOS
    {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    public const int
        SWP_NOMOVE = 0x0002,
        SWP_NOSIZE = 0x0001;

    public const int
        WM_WINDOWPOSCHANGING = 0x0046;
}