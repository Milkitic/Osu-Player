using System.Globalization;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.LayoutRenderers.Wrappers;

namespace Milki.OsuPlayer.Shared;

[LayoutRenderer("InvariantCulture")]
[ThreadAgnostic]
public sealed class InvariantCultureLayoutRendererWrapper : WrapperLayoutRendererBase
{
    protected override string Transform(string text)
    {
        return text;
    }

    protected override string RenderInner(LogEventInfo logEvent)
    {
        var task = Task.Run(() =>
        {
            var currentCulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                return base.RenderInner(logEvent);
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = currentCulture;
            }
        });

        return task.Result;
    }
}