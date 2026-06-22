using System;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Localization;

namespace OsuPlayer.Services;

public sealed class AppSettingsLanguagePreferenceStore : ILanguagePreferenceStore
{
    public string? GetLanguageCode()
    {
        return NormalizeLanguageCode(AppSettings.Default?.Interface.Locale);
    }

    public void SetLanguageCode(string languageCode)
    {
        if (AppSettings.Default is null)
        {
            return;
        }

        AppSettings.Default.Interface.Locale = languageCode;
        AppSettings.SaveDefault();
    }

    private static string? NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        if (string.Equals(languageCode, LanguageManager.SystemLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return LanguageManager.SystemLanguageCode;
        }

        if (languageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en";
        }

        return languageCode.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : languageCode;
    }
}
