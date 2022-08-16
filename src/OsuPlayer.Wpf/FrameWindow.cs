using System.Windows;

namespace Milki.OsuPlayer.Wpf;

public class FrameWindow : WindowEx
{
    private readonly WindowFrame? _frame;

    public FrameWindow(WindowFrame frame)
    {
        _frame = frame;
        _frame.Owner = this;
        Initialized += ZtDialog_Initialized;
        StateChanged += ZtDialog_StateChanged;
    }

    protected override void OnCanCloseChanged(object sender, RoutedEventArgs e)
    {
        if (_frame != null) _frame.CanClose = CanClose;
    }

    private void ZtDialog_StateChanged(object sender, EventArgs e)
    {
        if (_frame == null) return;
        if (WindowState == WindowState.Normal)
        {
            _frame.IsMax = false;
        }
        else if (WindowState == WindowState.Maximized)
        {
            _frame.IsMax = true;
        }
    }

    private void ZtDialog_Initialized(object sender, EventArgs e)
    {
        var oldContent = Content;
        if (_frame != null)
        {
            _frame.Child = oldContent;
            Content = null;
            Content = _frame;
        }

        SwitchWindowStyle();
    }

    private void SwitchWindowStyle()
    {
        if (WindowStyle != WindowStyle.ToolWindow) return;
        if (_frame == null) return;
        _frame.HasMax = false;
        _frame.HasMin = false;
    }
}