using System;
using System.Threading;
using System.Windows.Threading;

namespace Milky.WpfApi
{
    public static class Execute
    {
        private static void InnerExecute(Action action, Dispatcher dispatcher, bool waitForThread, bool force)
        {
            if (dispatcher == null)
            {
                dispatcher = Dispatcher.CurrentDispatcher;
            }

            if (dispatcher.CheckAccess() && !force)
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

        public static void OnUiThread(this Action action, bool force = false)
        {
            InnerExecute(action, null, true, force);
        }

        public static void CallUiThread(this Action action, bool force = false)
        {
            InnerExecute(action, null, false, force);
        }


        public static void OnUiThread(this Action action, SynchronizationContext uiContext)
        {
            uiContext.Send(obj => { action.Invoke(); }, null);
        }

        public static void OnUiThread(this Action action, Dispatcher dispatcher)
        {
            InnerExecute(action, dispatcher, false, false);
        }
    }
}
