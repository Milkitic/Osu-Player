using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Winforms
{
    internal abstract class GdipForm : Form
    {
        protected GdipForm(Bitmap bitmap)
        {
            #region Form Settings

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.MouseDown += OnMouseDown;

            #endregion Form Settings

            SetBitmap(bitmap);
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        /// <summary>
        /// 设置图象
        /// </summary>
        protected void SetBitmap(Bitmap bitmap, byte opacity = 255)
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                throw new ApplicationException("The bitmap must be 32bpp with alpha-channel.");

            this.ClientSize = new Size(bitmap.Width, bitmap.Height);
            // The idea of this is very simple,
            // 1. Create a compatible DC with screen;
            // 2. Select the bitmap with 32bpp with alpha-channel in the compatible DC;
            // 3. Call the UpdateLayeredWindow.

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));  // Grab a GDI handle from this GDI+ bitmap
                oldBitmap = SelectObject(memDc, hBitmap);
                ExtSize size = new ExtSize(bitmap.Width, bitmap.Height);
                ExtPoint pointSource = new ExtPoint(0, 0);
                ExtPoint topPos = new ExtPoint(this.Left, this.Top);
                ExtBlendFunction blend = new ExtBlendFunction
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = opacity,
                    AlphaFormat = AC_SRC_ALPHA
                };
                UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref pointSource, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);

                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);

                    // The documentation says that we have to use the Windows.DeleteObject...
                    // but since there is no such method I use the normal DeleteObject from Win32 GDI
                    // and it's working fine without any resource leak.
                    //
                    // Windows.DeleteObject(hBitmap); 
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
            bitmap.Dispose();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000; // This form has to have the WS_EX_LAYERED extended style
                return cp;
            }
        }

        #region Api Access Declare

        private enum Bool { False = 0, True }

        [StructLayout(LayoutKind.Sequential)]
        private struct ExtPoint
        {
            // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
            private readonly int x;
            // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
            private readonly int y;
            public ExtPoint(int x, int y)
            {
                this.x = x; this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ExtSize
        {
            // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
            private readonly int cx;
            // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
            private readonly int cy;
            public ExtSize(int cx, int cy)
            {
                this.cx = cx; this.cy = cy;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ExtArgb
        {
            private readonly byte Blue;
            private readonly byte Green;
            private readonly byte Red;
            private readonly byte Alpha;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ExtBlendFunction
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        // ReSharper disable once InconsistentNaming
        private const int WM_SYSCOMMAND = 0x0112;
        // ReSharper disable once InconsistentNaming
        private const int SC_MOVE = 0xF010;
        // ReSharper disable once InconsistentNaming
        private const int HTCAPTION = 0x0002;

        // ReSharper disable once InconsistentNaming
        private const int ULW_COLORKEY = 0x00000001;
        // ReSharper disable once InconsistentNaming
        private const int ULW_ALPHA = 0x00000002;
        // ReSharper disable once InconsistentNaming
        private const int ULW_OPAQUE = 0x00000004;

        // ReSharper disable once InconsistentNaming
        private const byte AC_SRC_OVER = 0x00;
        // ReSharper disable once InconsistentNaming
        private const byte AC_SRC_ALPHA = 0x01;

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern Bool UpdateLayeredWindow(IntPtr hWnd, IntPtr hdcDst, ref ExtPoint pptDst,
            ref ExtSize psize, IntPtr hDcSrc, ref ExtPoint pprSrc, int crKey, ref ExtBlendFunction pblend, int dwFlags);
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDc);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern Bool DeleteDC(IntPtr hDc);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDc, IntPtr hObject);
        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern Bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        #endregion
    }
}
