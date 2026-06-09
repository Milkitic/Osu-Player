using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using OsuPlayer.Shared;

namespace OsuPlayer.Avalonia.Interaction;

public static class Execute
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public static IUiThreadDispatcher UiThreadDispatcher { get; } = new AvaloniaUiThreadDispatcher();

    public static void OnUiThread(this Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            SafeInvoke(action);
        else
            Dispatcher.UIThread.Post(() => SafeInvoke(action), DispatcherPriority.Normal);
    }

    public static void ToUiThread(this Action action)
    {
        Dispatcher.UIThread.Post(() => SafeInvoke(action), DispatcherPriority.Normal);
    }

    public static Task OnUiThreadAsync(Func<Task> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return SafeInvokeAsync(action);

        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await SafeInvokeAsync(action);
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, DispatcherPriority.Normal);
        return tcs.Task;
    }

    public static bool CheckDispatcherAccess()
    {
        return Dispatcher.UIThread.CheckAccess();
    }

    private static void SafeInvoke(Action? action)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "UiContext execute error.");
        }
    }

    private static async Task SafeInvokeAsync(Func<Task>? action)
    {
        try
        {
            if (action != null)
                await action();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "UiContext execute error.");
            throw;
        }
    }

    private sealed class AvaloniaUiThreadDispatcher : IUiThreadDispatcher
    {
        public void Send(Action action) => OnUiThread(action);
        public void Post(Action action) => ToUiThread(action);
    }
}
