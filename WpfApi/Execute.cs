using System;
using System.Threading;
using System.Windows.Threading;

namespace Milkitic.WpfApi
{
    public static class Execute
    {
        private static void InnerExecute(Action action, Dispatcher dispatcher, bool waitForThread)
        {
            if (dispatcher == null)
            {
                dispatcher = Dispatcher.CurrentDispatcher;
            }

            if (dispatcher.CheckAccess())
            {
                action.Invoke();
            }
            else
            {
                if (waitForThread)
                    dispatcher.Invoke(action);
                else
                    dispatcher.BeginInvoke(action);
            }
        }

        public static void OnUiThread(this Action action)
        {
            InnerExecute(action, null, true);
        }

        public static void CallUiThread(this Action action)
        {
            InnerExecute(action, null, false);
        }

        public static void OnUiThread(this Action action, SynchronizationContext uiContext)
        {
            uiContext.Send(obj => { action.Invoke(); }, null);
        }

        public static void OnUiThread(this Action action, Dispatcher dispatcher)
        {
            InnerExecute(action, dispatcher, false);
        }
    }
}
