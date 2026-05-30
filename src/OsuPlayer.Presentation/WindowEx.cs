using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Milky.OsuPlayer.Presentation
{
    /// <summary>
    /// 扩展窗体基础类
    /// </summary>
    public abstract class WindowEx : Window, IWindowBase
    {
        private static readonly List<WindowEx> Current = new List<WindowEx>();
        private bool _shown;

        /// <summary>
        /// 窗体显示事件
        /// </summary>
        public static readonly RoutedEvent ShownEvent = EventManager.RegisterRoutedEvent
            ("Shown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WindowEx));

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
        /// 当前活跃的窗口。
        /// </summary>
        public static IEnumerable<WindowEx> CurrentWindows => new ReadOnlyCollection<WindowEx>(Current);

        /// <summary>
        /// 初始化 <see cref="WindowEx" /> 类的新实例。
        /// </summary>
        public WindowEx()
        {
            Closing += WindowEx_Closing;
            Closed += WindowEx_Closed;
            Current.Add(this);
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
        public static T GetCurrentOnly<T>() where T : WindowEx
        {
            return (T)CurrentWindows.Single(k => k.GetType() == typeof(T));
        }

        /// <summary>
        /// 获取第一个指定打开的窗体
        /// </summary>
        /// <typeparam name="T"><see cref="WindowEx" /> 的实例。</typeparam>
        /// <returns>获取的窗体</returns>
        public static T GetCurrentFirst<T>() where T : WindowEx
        {
            return (T)CurrentWindows.FirstOrDefault(k => k.GetType() == typeof(T));
        }

        private void WindowEx_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Application.Current.MainWindow != this) return;

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

        private void WindowEx_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
            Closed -= WindowEx_Closed;
            Closing -= WindowEx_Closing;
            Current.Remove(this);
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
    }
}
