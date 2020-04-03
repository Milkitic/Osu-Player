using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Common
{
    /// <summary>Provides a mechanism for releasing unmanaged resources asynchronously.</summary>
    public interface IAsyncDisposable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or
        /// resetting unmanaged resources asynchronously.
        /// </summary>
        Task DisposeAsync();
    }
}
