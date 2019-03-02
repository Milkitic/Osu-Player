using System.Windows.Forms;
using System.Windows.Input;

namespace Milky.OsuPlayer.Utils
{
    static class KeyConverter
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

        public static string ConvertToString(this Keys key)
        {
            string keyStr = key.ToString();
            if (keyStr.StartsWith("D") && keyStr.Length == 2)
                return keyStr[1].ToString();
            if (keyStr.StartsWith("NumPad") && keyStr.Length == 7)
                return "Num" + keyStr[6];
            switch (key)
            {
                case Keys.OemMinus:
                    return "-";
                case Keys.Oemplus:
                    return "=";
                case Keys.OemQuestion:
                    return "/";
                case Keys.Oem3:
                    return "`";
                case Keys.Oemcomma:
                    return ",";
                case Keys.OemPeriod:
                    return ".";
                case Keys.Oem1:
                    return ";";
                case Keys.OemQuotes:
                    return "'";
                case Keys.OemOpenBrackets:
                    return "[";
                case Keys.Oem6:
                    return "]";
                case Keys.Oem5:
                    return "\\";
                case Keys.Divide:
                    return "Num/";
                case Keys.Multiply:
                    return "Num*";
                case Keys.Subtract:
                    return "Num-";
                case Keys.Add:
                    return "Num+";
                case Keys.Decimal:
                    return "Num.";
                case Keys.Left:
                    return "←";
                case Keys.Right:
                    return "→";
                case Keys.Up:
                    return "↑";
                case Keys.Down:
                    return "↓";
                default:
                    return keyStr;
            }
        }
    }
}
