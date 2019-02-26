using DMSkin.WPF;
using Milky.WpfApi;
using System;

namespace Milky.OsuPlayer.Windows
{
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
}