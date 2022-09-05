using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;

namespace Milki.OsuPlayer.Wpf;

/// <summary>
/// 扩展窗体基础类
/// </summary>
public class WindowEx : Window, IWindowBase
{
    private static readonly SemaphoreSlim SingleThreadDialogSemaphore = new(1, 1);
    private static readonly List<WindowEx> Currents = new();

    public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register("CanClose",
        typeof(bool),
        typeof(WindowEx),
        new PropertyMetadata(true, (d, e) =>
        {
            if (d is WindowEx ex) ex.OnCanCloseChanged(ex, new RoutedEventArgs());
        }));

    private bool _shown;
    private bool _closing;
    private bool _closeRecalling;
    private bool? _cacheDialogResult;

    /// <summary>
    /// 窗体显示事件
    /// </summary>
    public static readonly RoutedEvent ShownEvent = EventManager.RegisterRoutedEvent("Shown",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(WindowEx));

    public static readonly RoutedEvent FirstLoadedEvent = EventManager.RegisterRoutedEvent("FirstLoaded",
        RoutingStrategy.Direct,
        typeof(RoutedEventHandler),
        typeof(WindowEx));

    public event RoutedEventHandler FirstLoaded
    {
        add => AddHandler(FirstLoadedEvent, value);
        remove => RemoveHandler(FirstLoadedEvent, value);
    }

    /// <summary>
    /// 当窗体显示时发生。
    /// </summary>
    public event RoutedEventHandler Shown
    {
        add => AddHandler(ShownEvent, value);
        remove => RemoveHandler(ShownEvent, value);
    }

    /// <summary>
    /// 窗体是否已经关闭。
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// 窗口句柄 (HWND)
    /// </summary>
    public IntPtr Handle { get; private set; }

    public bool CanClose
    {
        get => (bool)GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    /// <summary>
    /// 当前活跃的窗口。
    /// </summary>
    public static IEnumerable<WindowEx> CurrentWindows => new ReadOnlyCollection<WindowEx>(Currents);

    /// <summary>
    /// 初始化 <see cref="WindowEx" /> 类的新实例。
    /// </summary>
    public WindowEx()
    {
        Closing += WindowBase_Closing;
        Closed += WindowBase_Closed;

        void RoutedEventHandler(object sender, RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper(this).Handle;
            Loaded -= RoutedEventHandler;
            var args = new RoutedEventArgs(FirstLoadedEvent, this);
            RaiseEvent(args);
        }

        Loaded += RoutedEventHandler;
        Currents.Add(this);
    }

    public async Task ShowDialogAsync(CancellationToken cancellationToken = default)
    {
        await SingleThreadDialogSemaphore.WaitAsync(cancellationToken);
        var tcs = new TaskCompletionSource<object?>();
        Closed += LocalOnClosed;

        if (cancellationToken != default)
            cancellationToken.Register(() =>
            {
                SingleThreadDialogSemaphore.Release();
                tcs.SetCanceled();
            });

        await Execute.ToUiThreadAsync(Show);
        if (IsClosed) await Task.CompletedTask;
        else await tcs.Task;

        Closed -= LocalOnClosed;
        void LocalOnClosed(object? sender, EventArgs args)
        {
            SingleThreadDialogSemaphore.Release();
            tcs.TrySetResult(null);
        }
    }

    /// <summary>
    /// 当主窗体退出前，向所有活跃窗体发送退出请求
    /// </summary>
    /// <returns>返回是否可以关闭窗体</returns>
    protected virtual bool OnClosingRequest()
    {
        return true;
    }

    /// <summary>
    /// 获取唯一指定打开的窗体
    /// </summary>
    /// <typeparam name="T"><see cref="WindowEx" /> 的实例。</typeparam>
    /// <exception cref="InvalidOperationException">
    ///   没有元素满足该条件在 <see cref="T:Enumerable.Single" />。
    /// 
    ///   - 或 -
    /// 
    ///   多个元素满足该条件在 <see cref="T:Enumerable.Single" />。
    /// 
    ///   - 或 -
    /// 
    ///   源序列为空。
    /// </exception>
    /// <returns>获取的窗体</returns>
    public static T InstanceOnly<T>() where T : WindowEx
    {
        return (T)CurrentWindows.Single(k => k.GetType() == typeof(T));
    }

    /// <summary>
    /// 获取第一个指定打开的窗体
    /// </summary>
    /// <typeparam name="T"><see cref="WindowEx" /> 的实例。</typeparam>
    /// <returns>获取的窗体</returns>
    public static T InstanceFirst<T>() where T : WindowEx
    {
        return (T)CurrentWindows.First(k => k.GetType() == typeof(T));
    }

    /// <summary>
    ///   引发 <see cref="E:System.Windows.Window.ContentRendered" /> 事件。
    /// </summary>
    /// <param name="e">
    ///   包含事件数据的 <see cref="T:System.EventArgs" />。
    /// </param>
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        if (_shown)
            return;

        _shown = true;

        var args = new RoutedEventArgs(ShownEvent, this);
        RaiseEvent(args);
    }

    protected virtual Task<bool> OnAsyncClosing()
    {
        return Task.FromResult(true);
    }

    protected virtual void OnCanCloseChanged(object sender, RoutedEventArgs e)
    {
    }

    private async void WindowBase_Closing(object? sender, CancelEventArgs e)
    {
        if (!CanClose)
        {
            e.Cancel = true;
            return;
        }

        if (_closeRecalling) return;

        if (ComponentDispatcher.IsThreadModal)
            _cacheDialogResult = DialogResult;

        e.Cancel = true;

        if (_closing) return;
        _closing = true;
        await StartAsyncClosing(e);
    }

    private async Task StartAsyncClosing(CancelEventArgs e)
    {
        try
        {
            var result = await OnAsyncClosing();
            if (!result) return;
            result = CheckAllSubWindowsState();
            if (!result) return;

            _closeRecalling = true;
            await Dispatcher.InvokeAsync(() =>
            {
                if (ComponentDispatcher.IsThreadModal && _cacheDialogResult != null)
                    DialogResult = _cacheDialogResult;
                else
                    Close();
            });
        }
        finally
        {
            _closing = false;
        }
    }

    private void WindowBase_Closed(object? sender, EventArgs e)
    {
        if (Application.Current.MainWindow == this)
        {
            var windows = CurrentWindows.Where(k => k != this).ToList();
            foreach (var windowBase in windows)
            {
                if (windowBase is ToolWindow tw)
                {
                    tw.ForceClose();
                }
                else
                {
                    windowBase.Close();
                }
            }
        }

        IsClosed = true;
        Closed -= WindowBase_Closed;
        Closing -= WindowBase_Closing;
        Currents.Remove(this);
    }

    private bool CheckAllSubWindowsState()
    {
        if (Application.Current.MainWindow != this) return true;

        var windows = CurrentWindows.Where(k => k != this).ToList();
        if (windows.Any(windowBase => !windowBase.OnClosingRequest()))
        {
            return false;
        }

        return true;
    }
}