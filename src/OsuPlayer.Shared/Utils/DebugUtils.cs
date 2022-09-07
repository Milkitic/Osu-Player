using System.Diagnostics;
using System.Text;
using NLog;

namespace Milki.OsuPlayer.Shared.Utils;

public static class DebugUtils
{
    private const string Normal = "│ ";
    private const string Middle = "├─ ";
    private const string Last = "└─ ";

    public static string ToFullTypeMessage(this Exception exception)
    {
        return ExceptionToFullMessage(exception, new StringBuilder(), 0, true, true)!;
    }

    public static string ToSimpleTypeMessage(this Exception exception)
    {
        return ExceptionToFullMessage(exception, new StringBuilder(), 0, true, false)!;
    }

    public static string ToMessage(this Exception exception)
    {
        return ExceptionToFullMessage(exception, new StringBuilder(), 0, true, null)!;
    }

    public static void InvokeAndPrint(Action method, string caller = "anonymous method")
    {
        var sw = Stopwatch.StartNew();
        method?.Invoke();
        sw.Stop();
        Console.WriteLine($"[{caller}] Executed in {sw.Elapsed.TotalMilliseconds:#0.000} ms");
    }

    public static T InvokeAndPrint<T>(Func<T> method, string caller = "anonymous method")
    {
        var sw = Stopwatch.StartNew();
        var value = method.Invoke();
        sw.Stop();
        Console.WriteLine($"[{caller}] Executed in {sw.Elapsed.TotalMilliseconds:#0.000} ms");
        return value;
    }

    public static IDisposable CreateTimer(string name, ILogger? logger = null)
    {
        return new TimerImpl(name, logger);
    }

    private static string? ExceptionToFullMessage(Exception exception, StringBuilder stringBuilder, int deep,
        bool isLastItem, bool? includeFullType)
    {
        var hasChild = exception.InnerException != null;
        if (deep > 0)
        {
            for (int i = 0; i < deep; i++)
            {
                if (i == deep - 1)
                {
                    stringBuilder.Append((isLastItem && !hasChild) ? Last : Middle);
                }
                else
                {
                    stringBuilder.Append(Normal + " ");
                }
            }
        }

        var agg = exception as AggregateException;
        if (includeFullType == true)
        {
            var prefix = agg == null ? exception.GetType().ToString() : "!!AggregateException";
            stringBuilder.Append($"{prefix}: {GetTrueExceptionMessage(exception)}");
        }
        else if (includeFullType == false)
        {
            var prefix = exception.GetType().Name;
            stringBuilder.Append($"{prefix}: {GetTrueExceptionMessage(exception)}");
        }
        else
        {
            stringBuilder.Append(GetTrueExceptionMessage(exception));
        }

        stringBuilder.AppendLine();
        if (!hasChild)
        {
            return deep == 0 ? stringBuilder.ToString().Trim() : null;
        }

        if (agg != null)
        {
            for (int i = 0; i < agg.InnerExceptions.Count; i++)
            {
                ExceptionToFullMessage(agg.InnerExceptions[i], stringBuilder, deep + 1,
                    i == agg.InnerExceptions.Count - 1, includeFullType);
            }
        }
        else
        {
            ExceptionToFullMessage(exception.InnerException!, stringBuilder, deep + 1, true, includeFullType);
        }

        return deep == 0 ? stringBuilder.ToString().Trim() : null;

        static string GetTrueExceptionMessage(Exception ex)
        {
            if (ex is AggregateException { InnerException: { } } agg)
            {
                var complexMessage = agg.Message;
                var i = complexMessage.IndexOf(agg.InnerException.Message, StringComparison.Ordinal);
                if (i == -1)
                    return complexMessage;
                return complexMessage.Substring(0, i - 2);
            }

            return string.IsNullOrWhiteSpace(ex.Message) ? "{Empty Message}" : ex.Message;
        }
    }

    private class TimerImpl : IDisposable
    {
        private readonly ILogger? _logger;
        private readonly string _name;
        private readonly Stopwatch _sw;

        public TimerImpl(string name, ILogger? logger)
        {
            _name = name;
            _logger = logger;
            Print($"[{_name}] executing");
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            Print($"[{_name}] executed in {_sw.Elapsed.TotalMilliseconds:#0.000}ms");
        }

        private void Print(string message)
        {
            if (_logger == null)
            {
                Console.WriteLine(message);
            }
            else
            {
                _logger.Debug(message);
            }
        }
    }
}