using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace OsuPlayer.Presentation;

public abstract class WindowEx : Window, IWindowBase
{
    private static readonly List<WindowEx> s_current = new List<WindowEx>();
    private bool _shown;

    public event EventHandler? Shown;

    public bool IsClosed { get; set; }

    public static IEnumerable<WindowEx> CurrentWindows => new ReadOnlyCollection<WindowEx>(s_current);

    public WindowEx()
    {
        Closing += WindowEx_Closing;
        Closed += WindowEx_Closed;
        s_current.Add(this);
    }

    protected virtual bool RequestClose()
    {
        return true;
    }

    public static T GetCurrentOnly<T>() where T : WindowEx
    {
        return (T)CurrentWindows.Single(k => k.GetType() == typeof(T));
    }

    public static T GetCurrentFirst<T>() where T : WindowEx
    {
        return (T)CurrentWindows.FirstOrDefault(k => k.GetType() == typeof(T));
    }

    private static Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    private void WindowEx_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (GetMainWindow() != this) return;

        var windows = CurrentWindows.Where(k => k != this).ToList();
        if (windows.Any(windowEx => !windowEx.RequestClose()))
        {
            e.Cancel = true;
            return;
        }

        foreach (var windowBase in windows)
        {
            windowBase.Close();
        }
    }

    private void WindowEx_Closed(object? sender, EventArgs e)
    {
        IsClosed = true;
        Closed -= WindowEx_Closed;
        Closing -= WindowEx_Closing;
        s_current.Remove(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_shown)
            return;

        _shown = true;
        Shown?.Invoke(this, EventArgs.Empty);
    }
}