using System.Runtime.InteropServices;
using System.Windows;

namespace Milki.OsuPlayer.Windows;

public partial class LyricWindow
{
    private const uint WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int GWL_EXSTYLE = (-20);

    private bool _isLocked;
    private uint _oldGwlEx;

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            _isLocked = value;
            SharedVm.Default.IsLyricWindowLocked = value;
            if (_isLocked)
            {
                SetPenetrate();
                TbLyric.Opacity = 0.9;
                //ShowAnimation();
                _viewModel.ShowFrame = false;
            }
            else
            {
                ReleasePenetrate();
                TbLyric.Opacity = 1;
            }
        }
    }

    [DllImport("user32", EntryPoint = "SetWindowLong")]
    private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

    [DllImport("user32", EntryPoint = "GetWindowLong")]
    private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

    private void WindowBase_Loaded(object sender, RoutedEventArgs e)
    {
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