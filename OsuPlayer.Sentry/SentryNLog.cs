using NLog;
using NLog.Config;
using Sentry;

namespace Milky.OsuPlayer.Sentry
{
    public static partial class SentryNLog
    {
        public static void Init(LoggingConfiguration config)
        {
            // https://github.com/mkaring/ConfuserEx
            // https://docs.sentry.io/platforms/dotnet/nlog/
            
            config.AddSentry(o =>
            {
                o.Dsn = new Dsn(__dsn);
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

                o.Environment = "PRODUCTION";
                o.AttachStacktrace = true;
                o.SendDefaultPii = true;
                o.ShutdownTimeoutSeconds = 5;
                o.IncludeEventDataOnBreadcrumbs = true;
            });
            LogManager.ReconfigExistingLoggers();
        }
    }
}