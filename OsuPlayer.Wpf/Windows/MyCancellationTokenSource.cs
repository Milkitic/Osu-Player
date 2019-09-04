using System;
using System.Threading;

namespace Milky.OsuPlayer.Windows
{
    internal class MyCancellationTokenSource : CancellationTokenSource
    {
        public Guid Guid { get; }

        public MyCancellationTokenSource()
        {
            Guid = Guid.NewGuid();
        }
    }
}