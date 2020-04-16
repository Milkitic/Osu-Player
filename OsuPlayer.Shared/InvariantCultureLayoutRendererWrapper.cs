﻿using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        protected override string Transform(string text)
        {
            return text;
        }

        protected override string RenderInner(LogEventInfo logEvent)
        {
            if (Application.Current?.MainWindow is null)
                return base.RenderInner(logEvent);

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
}