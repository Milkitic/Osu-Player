using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Milky.OsuPlayer.Presentation.Interaction
{
    public static class Execute
    {
        private static Dispatcher _uiDispatcher;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static void SetMainThreadContext()
        {
            if (_uiDispatcher != null) Logger.Warn("Current dispatcher may be replaced.");
            _uiDispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        public static void OnUiThread(this Action action)
        {
            var dispatcher = GetDispatcher();
            if (dispatcher == null || dispatcher.CheckAccess())
                SafeInvoke(action);
            else
                dispatcher.Invoke(() => SafeInvoke(action));
        }

        public static void ToUiThread(this Action action)
        {
            var dispatcher = GetDispatcher();
            if (dispatcher == null)
            {
                SafeInvoke(action);
                return;
            }

            dispatcher.BeginInvoke(new Action(() => SafeInvoke(action)), DispatcherPriority.Normal);
        }

        public static Task OnUiThreadAsync(Func<Task> action)
        {
            var dispatcher = GetDispatcher();
            if (dispatcher == null || dispatcher.CheckAccess())
                return SafeInvokeAsync(action);

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            dispatcher.BeginInvoke(new Action(async () =>
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
            }), DispatcherPriority.Normal);
            return tcs.Task;
        }

        public static bool CheckDispatcherAccess()
        {
            var dispatcher = GetDispatcher();
            return dispatcher == null
                ? Thread.CurrentThread.ManagedThreadId == 1
                : dispatcher.CheckAccess();
        }

        private static Dispatcher GetDispatcher()
        {
            return _uiDispatcher ?? Application.Current?.Dispatcher;
        }

        private static void SafeInvoke(Action action)
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

        private static async Task SafeInvokeAsync(Func<Task> action)
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
    }
}