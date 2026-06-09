using System.Windows.Input;
using Milki.Extensions.MouseKeyHook;

namespace OsuPlayer.Utils;

internal static class KeyConverter
{
    public static string ConvertToString(this Key key)
    {
        string keyStr = key.ToString();
        if (keyStr.StartsWith("D") && keyStr.Length == 2)
            return keyStr[1].ToString();

        if (keyStr.StartsWith("NumPad") && keyStr.Length == 7)
            return "Num" + keyStr[6];

        switch (key)
        {
            case Key.OemMinus:
                return "-";
            case Key.OemPlus:
                return "=";
            case Key.OemQuestion:
                return "/";
            case Key.Oem3:
                return "`";
            case Key.OemComma:
                return ",";
            case Key.OemPeriod:
                return ".";
            case Key.Oem1:
                return ";";
            case Key.OemQuotes:
                return "'";
            case Key.OemOpenBrackets:
                return "[";
            case Key.Oem6:
                return "]";
            case Key.Oem5:
                return "\\";
            case Key.Divide:
                return "Num/";
            case Key.Multiply:
                return "Num*";
            case Key.Subtract:
                return "Num-";
            case Key.Add:
                return "Num+";
            case Key.Decimal:
                return "Num.";
            case Key.Left:
                return "←";
            case Key.Right:
                return "→";
            case Key.Up:
                return "↑";
            case Key.Down:
                return "↓";
            default:
                return keyStr;
        }
    }

    public static string ConvertToString(this HookKeys key)
    {
        string keyStr = key.ToString();
        if (keyStr.StartsWith("D") && keyStr.Length == 2)
            return keyStr[1].ToString();

        if (keyStr.StartsWith("NumPad") && keyStr.Length == 7)
            return "Num" + keyStr[6];

        switch (key)
        {
            case HookKeys.OemMinus:
                return "-";
            case HookKeys.Oemplus:
                return "=";
            case HookKeys.OemQuestion:
                return "/";
            case HookKeys.Oem3:
                return "`";
            case HookKeys.Oemcomma:
                return ",";
            case HookKeys.OemPeriod:
                return ".";
            case HookKeys.Oem1:
                return ";";
            case HookKeys.OemQuotes:
                return "'";
            case HookKeys.OemOpenBrackets:
                return "[";
            case HookKeys.Oem6:
                return "]";
            case HookKeys.Oem5:
                return "\\";
            case HookKeys.Divide:
                return "Num/";
            case HookKeys.Multiply:
                return "Num*";
            case HookKeys.Subtract:
                return "Num-";
            case HookKeys.Add:
                return "Num+";
            case HookKeys.Decimal:
                return "Num.";
            case HookKeys.Left:
                return "←";
            case HookKeys.Right:
                return "→";
            case HookKeys.Up:
                return "↑";
            case HookKeys.Down:
                return "↓";
            default:
                return keyStr;
        }
    }
}