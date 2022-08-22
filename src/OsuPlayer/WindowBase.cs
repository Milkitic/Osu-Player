using System.Windows;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer;

public class WindowBase : WindowEx
{
    public WindowBase()
    {
        StateChanged += WindowBase_StateChanged;
    }

    protected bool IsForceExitState { get; private set; }
    protected WindowState LastWindowsState { get; private set; }

    private void WindowBase_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized) return;
        LastWindowsState = WindowState;
    }

    public void ForceClose()
    {
        IsForceExitState = true;
        Close();
    }
}