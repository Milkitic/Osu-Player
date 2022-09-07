using System.Text;
using Milki.Extensions.MouseKeyHook;

namespace Milki.OsuPlayer.Shared.Models;

public class BindKeys
{
    private BindKeys(HookModifierKeys modifierKeys, HookKeys? keys)
    {
        ModifierKeys = modifierKeys;
        Keys = keys;
    }

    public HookKeys? Keys { get; }

    public HookModifierKeys ModifierKeys { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (ModifierKeys.HasFlag(HookModifierKeys.Control))
        {
            sb.Append("Ctrl");
        }

        if (ModifierKeys.HasFlag(HookModifierKeys.Shift))
        {
            if (sb.Length > 0)
            {
                sb.Append('+');
            }

            sb.Append("Shift");
        }

        if (ModifierKeys.HasFlag(HookModifierKeys.Alt))
        {
            if (sb.Length > 0)
            {
                sb.Append('+');
            }

            sb.Append("Alt");
        }

        if (Keys != null)
        {
            if (sb.Length > 0)
            {
                sb.Append('+');
            }

            sb.Append(Keys.ToString());
        }

        return sb.ToString();
    }

    public static BindKeys Parse(string str)
    {
        var modifierKeys = HookModifierKeys.None;
        HookKeys? keys = null;
        var split = str.Split('+', StringSplitOptions.RemoveEmptyEntries);
        foreach (var s in split)
        {
            if (s.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            {
                modifierKeys |= HookModifierKeys.Control;
            }
            else if (s.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifierKeys |= HookModifierKeys.Shift;
            }
            else if (s.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifierKeys |= HookModifierKeys.Alt;
            }
            else if (keys == null)
            {
                keys = Enum.Parse<HookKeys>(s);
            }
        }

        return new BindKeys(modifierKeys, keys);
    }
}