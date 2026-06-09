using System;

namespace OsuPlayer.UiComponents.FrontDialogComponent;

public class DialogClosingEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}