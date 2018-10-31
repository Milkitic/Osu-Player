using System;
using System.Windows.Threading;

namespace Milkitic.WpfApi
{
    public static class Execute
    {
        private static Action<Action> _executor = action => action();

        /// <summary>
        /// 初始化UI调度器
        /// </summary>
        public static void InitializeWithDispatcher()
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            _executor = action =>
            {
                if (dispatcher.CheckAccess())
                    action();
                else dispatcher.BeginInvoke(action);
            };
        }

        /// <summary>
        /// UI线程中执行方法
        /// </summary>
        public static void OnUiThread(this Action action)
        {
            _executor(action);
        }
    }
}
