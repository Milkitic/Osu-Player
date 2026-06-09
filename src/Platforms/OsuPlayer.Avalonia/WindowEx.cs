using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;

namespace OsuPlayer.Avalonia;

/// <summary>
/// 扩展窗体基础类（Avalonia 版）
/// </summary>
public abstract class WindowEx : Window
{
    private static readonly List<WindowEx> s_current = new List<WindowEx>();
    private bool _shown;

    /// <summary>
    /// 当窗体显示时发生。
    /// </summary>
    public event EventHandler? Shown;

    /// <summary>
    /// 窗体是否已经关闭。
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// 当前活跃的窗口。
    /// </summary>
    public static IEnumerable<WindowEx> CurrentWindows => new ReadOnlyCollection<WindowEx>(s_current);

    /// <summary>
    /// 初始化 <see cref="WindowEx" /> 类的新实例。
    /// </summary>
    public WindowEx()
    {
        s_current.Add(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        if (!_shown)
        {
            _shown = true;
            Shown?.Invoke(this, EventArgs.Empty);
        }

        base.OnOpened(e);
    }

    /// <summary>
    /// 当主窗体退出前，向所有活跃窗体发送退出请求
    /// </summary>
    /// <returns>返回是否可以关闭窗体</returns>
    protected virtual bool RequestClose()
    {
        return true;
    }

    /// <summary>
    /// 获取唯一指定打开的窗体
    /// </summary>
    public static T? GetCurrentOnly<T>() where T : WindowEx
    {
        return CurrentWindows.SingleOrDefault(k => k.GetType() == typeof(T)) as T;
    }

    /// <summary>
    /// 获取第一个指定打开的窗体
    /// </summary>
    public static T? GetCurrentFirst<T>() where T : WindowEx
    {
        return CurrentWindows.FirstOrDefault(k => k.GetType() == typeof(T)) as T;
    }

    protected override void OnClosed(EventArgs e)
    {
        IsClosed = true;
        s_current.Remove(this);
        base.OnClosed(e);
    }
}
