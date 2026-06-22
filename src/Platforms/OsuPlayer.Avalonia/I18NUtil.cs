using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OsuPlayer.Localization;

namespace OsuPlayer;

public static class I18NUtil
{
    public static Dictionary<string, string> AvailableLangDic { get; } = new()
    {
        { "System Default", LanguageManager.SystemLanguageCode },
        { "中文", "zh-CN" },
        { "English", "en" }
    };

    public static void LoadI18N()
    {
    }

    public static void SwitchToLang(string locale)
    {
        LocalizationService.Instance.ApplyCulture(LanguageManager.ResolveCulture(locale));
    }

    public static string GetString(string key)
    {
        return LocalizationService.Instance[NormalizeLegacyKey(key)];
    }

    private static string NormalizeLegacyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(key.Length);
        var upperNext = true;
        foreach (var ch in key)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(upperNext ? char.ToUpper(ch, CultureInfo.InvariantCulture) : ch);
                upperNext = false;
                continue;
            }

            if (builder.Length > 0 && builder[^1] != '_')
            {
                builder.Append('_');
            }

            upperNext = true;
        }

        if (builder.Length > 0 && builder[^1] == '_')
        {
            builder.Length--;
        }

        return builder.Length > 0 && char.IsDigit(builder[0])
            ? "_" + builder
            : builder.ToString();
    }
}
