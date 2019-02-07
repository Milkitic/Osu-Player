using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Milky.OsuPlayer.Windows
{
    partial class LyricWindow
    {
        private uint _oldGwlEx;
        private bool _isLocked;
        private const uint WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        private IntPtr Handle { get; set; }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                _isLocked = value;
                _mainWindow.ViewModel.IsLyricWindowLocked = value;
                if (_isLocked)
                {
                    SetPenetrate();
                    ImgLyric.Opacity = 0.9;
                    //ShowAnimation();
                    HideFrame();
                }
                else
                {
                    ReleasePenetrate();
                    ImgLyric.Opacity = 1;
                }
            }
        }

        private void WindowBase_Loaded(object sender, RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;
            _oldGwlEx = GetWindowLong(this.Handle, GWL_EXSTYLE);
        }

        private void SetPenetrate()
        {
            uint newGwlEx = SetWindowLong(this.Handle, GWL_EXSTYLE, WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void ReleasePenetrate()
        {
            uint newGwlEx = SetWindowLong(this.Handle, GWL_EXSTYLE, _oldGwlEx);
        }
    }
}
