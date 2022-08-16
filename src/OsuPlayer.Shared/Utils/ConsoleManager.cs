using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Milki.OsuPlayer.Shared.Utils;

[SuppressUnmanagedCodeSecurity]
public static class ConsoleManager
{
    // ReSharper disable InconsistentNaming
    [Flags]
    public enum CharacterAttributes
    {
        FOREGROUND_BLUE = 0x0001,
        FOREGROUND_GREEN = 0x0002,
        FOREGROUND_RED = 0x0004,
        FOREGROUND_INTENSITY = 0x0008,
        BACKGROUND_BLUE = 0x0010,
        BACKGROUND_GREEN = 0x0020,
        BACKGROUND_RED = 0x0040,
        BACKGROUND_INTENSITY = 0x0080,
        COMMON_LVB_LEADING_BYTE = 0x0100,
        COMMON_LVB_TRAILING_BYTE = 0x0200,
        COMMON_LVB_GRID_HORIZONTAL = 0x0400,
        COMMON_LVB_GRID_LVERTICAL = 0x0800,
        COMMON_LVB_GRID_RVERTICAL = 0x1000,
        COMMON_LVB_REVERSE_VIDEO = 0x4000,
        COMMON_LVB_UNDERSCORE = 0x8000
    }

    private static ConsoleEventDelegate _handler;
    private delegate bool ConsoleEventDelegate(int eventType);
    public static bool HasConsole => GetConsoleWindow() != IntPtr.Zero;

    private const string Kernel32_DllName = "kernel32.dll";
    // ReSharper restore InconsistentNaming

    [DllImport(Kernel32_DllName)]
    private static extern bool AllocConsole();

    [DllImport(Kernel32_DllName)]
    private static extern bool FreeConsole();

    [DllImport(Kernel32_DllName)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport(Kernel32_DllName)]
    private static extern int GetConsoleOutputCP();
    [DllImport(Kernel32_DllName)]
    private static extern int SetConsoleTextAttribute(IntPtr hConsoleOutput,
        CharacterAttributes wAttributes);
    [DllImport(Kernel32_DllName, SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

    private const int MF_BYCOMMAND = 0x00000000;
    public const int SC_CLOSE = 0xF060;

    [DllImport("user32.dll")]
    public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    /// <summary>
    /// Creates a new console instance if the process is not attached to a console already.
    /// </summary>
    public static void Show()
    {
        if (HasConsole) return;
        AllocConsole();
        InvalidateOutAndError();
        Console.Title = "KeyASIO Debugging Console";
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Note: Closing this window will lead to program exiting.");
        Console.ResetColor();
        var hMenu = GetSystemMenu(GetConsoleWindow(), false);
        DeleteMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
    }

    public static void BindExitAction(Action exitAction)
    {
        if (exitAction == null || _handler != null) return;
        _handler = eventType =>
        {
            if (eventType != 2) return false;
            exitAction();
            return true;
        };

        SetConsoleCtrlHandler(_handler, true);
    }

    /// <summary>
    /// If the process has a console attached to it, it will be detached and no longer visible. Writing to the System.Console is still possible, but no output will be shown.
    /// </summary>
    public static void Hide()
    {
        if (!HasConsole) return;
        SetOutAndErrorNull();
        FreeConsole();
    }

    private static void InvalidateOutAndError()
    {
        var type = typeof(Console);
        var fieldOut = type.GetField(
#if NETCOREAPP3_1_OR_GREATER
                "s_out",
#else
            "_out",
#endif

            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        var fieldError = type.GetField(
#if NETCOREAPP3_1_OR_GREATER
                "s_error",
#else
            "_error",
#endif
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        //System.Reflection.MethodInfo? _InitializeStdOutError = type.GetMethod("InitializeStdOutError",
        //    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Debug.Assert(fieldOut != null);
        Debug.Assert(fieldError != null);

        //Debug.Assert(_InitializeStdOutError != null);

        fieldOut.SetValue(null, null);
        fieldError.SetValue(null, null);

        _ = Console.Out;
        _ = Console.Error;
        //_InitializeStdOutError.Invoke(null, new object[] { true });
    }

    private static void SetOutAndErrorNull()
    {
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
    }
}