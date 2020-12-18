using NLog;
using NLog.Config;
using Sentry;
using System.Reflection;

namespace Milky.OsuPlayer.Sentry
{
    public static partial class SentryNLog
    {
        public static void Init(LoggingConfiguration config)
        {
            // https://github.com/mkaring/ConfuserEx
            // https://docs.sentry.io/platforms/dotnet/nlog/

            var t = typeof(SentryNLog);
            var field = t.GetField("__dsn", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField);
            if (field == null) return;
            string dsn = (string)field.GetValue(null);

            config.AddSentry(o =>
            {
                o.Dsn = new Dsn(dsn);
                //Optionally specify a separate format for message
                o.Layout = "${message}";
                // Optionally specify a separate format for breadcrumbs
                o.BreadcrumbLayout = "${logger}: ${message}";

                o.IgnoreEventsWithNoException = true;
                o.InitializeSdk = true;

                // Debug and higher are stored as breadcrumbs (default is Info)
                o.MinimumBreadcrumbLevel = LogLevel.Debug;
                // Error and higher is sent as event (default is Error)
                o.MinimumEventLevel = LogLevel.Error;
                // Send the logger name as a tag
                o.AddTag("logger", "${logger}");

                o.Environment = "Production";
                o.AttachStacktrace = true;
                o.SendDefaultPii = true;
                o.ShutdownTimeoutSeconds = 5;
                o.IncludeEventDataOnBreadcrumbs = true;
            });
            LogManager.ReconfigExistingLoggers();
        }
    }
}