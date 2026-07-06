using System;
using System.Collections.Generic;
using Avalonia.Input;
using Milki.Extensions.MouseKeyHook;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.Utils;

internal static class HotKeyTextHelper
{
    private static readonly Dictionary<Key, HookKeys> KeyMap = new()
    {
        [Key.OemMinus] = HookKeys.OemMinus,
        [Key.OemPlus] = HookKeys.Oemplus,
        [Key.OemQuestion] = HookKeys.OemQuestion,
        [Key.Oem3] = HookKeys.Oem3,
        [Key.OemComma] = HookKeys.Oemcomma,
        [Key.OemPeriod] = HookKeys.OemPeriod,
        [Key.Oem1] = HookKeys.Oem1,
        [Key.OemQuotes] = HookKeys.OemQuotes,
        [Key.OemOpenBrackets] = HookKeys.OemOpenBrackets,
        [Key.Oem6] = HookKeys.Oem6,
        [Key.Oem5] = HookKeys.Oem5,
        [Key.Divide] = HookKeys.Divide,
        [Key.Multiply] = HookKeys.Multiply,
        [Key.Subtract] = HookKeys.Subtract,
        [Key.Add] = HookKeys.Add,
        [Key.Decimal] = HookKeys.Decimal,
        [Key.Left] = HookKeys.Left,
        [Key.Right] = HookKeys.Right,
        [Key.Up] = HookKeys.Up,
        [Key.Down] = HookKeys.Down,
        [Key.Enter] = HookKeys.Enter,
        [Key.Return] = HookKeys.Enter,
        [Key.Space] = HookKeys.Space,
        [Key.Back] = HookKeys.Back,
        [Key.Tab] = HookKeys.Tab,
        [Key.Escape] = HookKeys.Escape,
        [Key.Delete] = HookKeys.Delete,
        [Key.Insert] = HookKeys.Insert,
        [Key.Home] = HookKeys.Home,
        [Key.End] = HookKeys.End,
        [Key.PageUp] = HookKeys.PageUp,
        [Key.PageDown] = HookKeys.PageDown,
    };

    public static string Format(HotKey hotKey)
    {
        if (hotKey == null || !hotKey.Enabled)
        {
            return string.Empty;
        }

        var strList = new List<string>();
        if (hotKey.UseControlKey)
        {
            strList.Add("Ctrl");
        }

        if (hotKey.UseShiftKey)
        {
            strList.Add("Shift");
        }

        if (hotKey.UseAltKey)
        {
            strList.Add("Alt");
        }

        strList.Add(ConvertToString(hotKey.Key));
        return string.Join(" + ", strList);
    }

    public static string FormatPreview(KeyModifiers modifiers, Key key)
    {
        var strList = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            strList.Add("Ctrl");
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            strList.Add("Shift");
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            strList.Add("Alt");
        }

        if (!IsModifierKey(key))
        {
            strList.Add(ConvertToString(key));
        }

        return strList.Count == 0 ? string.Empty : string.Join(" + ", strList);
    }

    public static bool TryConvert(Key key, out HookKeys hookKey)
    {
        if (KeyMap.TryGetValue(key, out hookKey))
        {
            return true;
        }

        return Enum.TryParse(key.ToString(), true, out hookKey);
    }

    public static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt;
    }

    private static string ConvertToString(Key key)
    {
        var keyStr = key.ToString();
        if (keyStr.StartsWith("D", StringComparison.Ordinal) && keyStr.Length == 2)
        {
            return keyStr[1].ToString();
        }

        if (keyStr.StartsWith("NumPad", StringComparison.Ordinal) && keyStr.Length == 7)
        {
            return "Num" + keyStr[6];
        }

        return key switch
        {
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemQuestion => "/",
            Key.Oem3 => "`",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.Oem1 => ";",
            Key.OemQuotes => "'",
            Key.OemOpenBrackets => "[",
            Key.Oem6 => "]",
            Key.Oem5 => "\\",
            Key.Divide => "Num/",
            Key.Multiply => "Num*",
            Key.Subtract => "Num-",
            Key.Add => "Num+",
            Key.Decimal => "Num.",
            Key.Left => "\u2190",
            Key.Right => "\u2192",
            Key.Up => "\u2191",
            Key.Down => "\u2193",
            _ => keyStr
        };
    }

    private static string ConvertToString(HookKeys key)
    {
        var keyStr = key.ToString();
        if (keyStr.StartsWith("D", StringComparison.Ordinal) && keyStr.Length == 2)
        {
            return keyStr[1].ToString();
        }

        if (keyStr.StartsWith("NumPad", StringComparison.Ordinal) && keyStr.Length == 7)
        {
            return "Num" + keyStr[6];
        }

        return key switch
        {
            HookKeys.OemMinus => "-",
            HookKeys.Oemplus => "=",
            HookKeys.OemQuestion => "/",
            HookKeys.Oem3 => "`",
            HookKeys.Oemcomma => ",",
            HookKeys.OemPeriod => ".",
            HookKeys.Oem1 => ";",
            HookKeys.OemQuotes => "'",
            HookKeys.OemOpenBrackets => "[",
            HookKeys.Oem6 => "]",
            HookKeys.Oem5 => "\\",
            HookKeys.Divide => "Num/",
            HookKeys.Multiply => "Num*",
            HookKeys.Subtract => "Num-",
            HookKeys.Add => "Num+",
            HookKeys.Decimal => "Num.",
            HookKeys.Left => "\u2190",
            HookKeys.Right => "\u2192",
            HookKeys.Up => "\u2191",
            HookKeys.Down => "\u2193",
            _ => keyStr
        };
    }
}
