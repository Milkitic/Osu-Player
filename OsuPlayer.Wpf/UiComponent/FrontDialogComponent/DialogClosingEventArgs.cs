using System;

namespace Milky.OsuPlayer.UiComponent.FrontDialogComponent
{
    public class DialogClosingEventArgs : EventArgs
    {
        public bool Cancel { get; set; }
    }
}