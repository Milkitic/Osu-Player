using DMSkin.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Milky.OsuPlayer.Windows
{
    public class WindowBase : Window, IWindowBase
    {
        public WindowBase()
        {
            Closed += WindowBase_Closed;
        }

        private void WindowBase_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
        }

        public bool IsClosed { get; set; }
    }

    public class EffectWindowBase : DMSkinSimpleWindow, IWindowBase
    {
        public EffectWindowBase()
        {
            Closed += WindowBase_Closed;
        }

        private void WindowBase_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
        }

        public bool IsClosed { get; set; }
    }

    public interface IWindowBase
    {
        bool IsClosed { get; set; }
    }
}
