using System;
using System.Threading;

namespace OsuPlayer.Shared;

public class MyCancellationTokenSource : CancellationTokenSource
{
    public Guid Guid { get; }

    public MyCancellationTokenSource()
    {
        Guid = Guid.NewGuid();
    }
}
