using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Milky.WpfApi
{
    public class WindowBase : Window, IWindowBase
    {
        private static readonly List<Window> Current = new List<Window>();

        public WindowBase()
        {
            Closed += WindowBase_Closed;
            Current.Add(this);
        }

        private void WindowBase_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
            Closed -= WindowBase_Closed;
            Current.Remove(this);
        }

        public bool IsClosed { get; set; }
        public Guid Guid { get; } = Guid.NewGuid();
        public static IReadOnlyList<Window> CurrentWindows => Current;

        public static Window GetCurrentOnly<T>() where T : WindowBase
        {
            return CurrentWindows.Single(k => k.GetType() == typeof(T));
        }

        public static Window GetCurrentFirst<T>() where T : WindowBase
        {
            return CurrentWindows.FirstOrDefault(k => k.GetType() == typeof(T));
        }
    }
}
