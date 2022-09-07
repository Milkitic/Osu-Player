using System.Windows.Forms;
using System.Windows.Input;

namespace Milki.OsuPlayer.Utils;

internal static class KeyConverter
{
    public static string ConvertToString(this Key key)
    {
        string keyStr = key.ToString();
        if (keyStr.StartsWith("D") && keyStr.Length == 2)
            return keyStr[1].ToString();

        if (keyStr.StartsWith("NumPad") && keyStr.Length == 7)
            return "Num" + keyStr[6];

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
            Key.Left => "←",
            Key.Right => "→",
            Key.Up => "↑",
            Key.Down => "↓",
            _ => keyStr
        };
    }

    public static string ConvertToString(this Keys key)
    {
        string keyStr = key.ToString();
        if (keyStr.StartsWith("D") && keyStr.Length == 2)
            return keyStr[1].ToString();

        if (keyStr.StartsWith("NumPad") && keyStr.Length == 7)
            return "Num" + keyStr[6];

        return key switch
        {
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.OemQuestion => "/",
            Keys.Oem3 => "`",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.Oem1 => ";",
            Keys.OemQuotes => "'",
            Keys.OemOpenBrackets => "[",
            Keys.Oem6 => "]",
            Keys.Oem5 => "\\",
            Keys.Divide => "Num/",
            Keys.Multiply => "Num*",
            Keys.Subtract => "Num-",
            Keys.Add => "Num+",
            Keys.Decimal => "Num.",
            Keys.Left => "←",
            Keys.Right => "→",
            Keys.Up => "↑",
            Keys.Down => "↓",
            _ => keyStr
        };
    }
}