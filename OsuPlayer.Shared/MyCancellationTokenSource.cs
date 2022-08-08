using System;
using System.Threading;

namespace Milki.OsuPlayer.Shared
{
    public class MyCancellationTokenSource : CancellationTokenSource
    {
        public Guid Guid { get; }

        public MyCancellationTokenSource()
        {
            Guid = Guid.NewGuid();
        }
    }
}