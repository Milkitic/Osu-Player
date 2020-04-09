using System.Globalization;
using System.Threading;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.LayoutRenderers.Wrappers;

namespace Milky.OsuPlayer.Shared
{
    [LayoutRenderer("InvariantCulture")]
    [ThreadAgnostic]
    public sealed class InvariantCultureLayoutRendererWrapper : WrapperLayoutRendererBase
    {
        private static readonly object CultureLockObject = new object();
        protected override string Transform(string text)
        {
            return text;
        }

        protected override string RenderInner(LogEventInfo logEvent)
        {
            lock (CultureLockObject)
            {
                var currentCulture = Thread.CurrentThread.CurrentUICulture;
                try
                {
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                    return base.RenderInner(logEvent);
                }
                finally
                {
                    Thread.CurrentThread.CurrentUICulture = currentCulture;
                }
            }
        }
    }
}