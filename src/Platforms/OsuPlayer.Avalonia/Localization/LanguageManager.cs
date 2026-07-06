using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OsuPlayer.Localization;

public partial class LanguageManager : ObservableObject
{
    public const string SystemLanguageCode = "system";
    private readonly ILanguagePreferenceStore _languagePreferenceStore;
    private bool _isUpdating;

    public LanguageManager(ILanguagePreferenceStore languagePreferenceStore)
    {
        _languagePreferenceStore = languagePreferenceStore;
        InitializeLanguages();
    }

    public ObservableCollection<LanguageItem> AvailableLanguages { get; } = [];

    [ObservableProperty]
    public partial LanguageItem? SelectedLanguageItem { get; set; }

    partial void OnSelectedLanguageItemChanged(LanguageItem? value)
    {
        if (value is null || _isUpdating)
        {
            return;
        }

        _languagePreferenceStore.SetLanguageCode(value.Code);
        ApplyLanguage(value.Code);
        RefreshAvailableLanguages(value.Code);
    }

    public static CultureInfo ResolveCulture(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ||
               string.Equals(languageCode, SystemLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.InstalledUICulture
            : new CultureInfo(languageCode);
    }

    private void InitializeLanguages()
    {
        var persistedCode = _languagePreferenceStore.GetLanguageCode();
        var savedCode = string.IsNullOrWhiteSpace(persistedCode)
            ? SystemLanguageCode
            : persistedCode;

        ApplyLanguage(savedCode);

        _isUpdating = true;
        try
        {
            PopulateLanguages();
            SelectedLanguageItem = AvailableLanguages.FirstOrDefault(x =>
                                       string.Equals(x.Code, savedCode, StringComparison.OrdinalIgnoreCase))
                                   ?? AvailableLanguages[0];
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void RefreshAvailableLanguages(string selectedCode)
    {
        _isUpdating = true;
        try
        {
            SelectedLanguageItem = null;
            AvailableLanguages.Clear();
            PopulateLanguages();
            SelectedLanguageItem =
                AvailableLanguages
                    .FirstOrDefault(x => string.Equals(x.Code, selectedCode, StringComparison.OrdinalIgnoreCase))
                ?? AvailableLanguages[0];
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void PopulateLanguages()
    {
        AvailableLanguages.Add(new LanguageItem
        {
            Name = LocalizationService.Instance["Language_SystemDefault"],
            Code = SystemLanguageCode
        });
        AvailableLanguages.Add(new LanguageItem
        {
            Name = CultureInfo.GetCultureInfo("zh-CN").NativeName,
            Code = "zh-CN"
        });
        AvailableLanguages.Add(new LanguageItem
        {
            Name = CultureInfo.GetCultureInfo("en").NativeName,
            Code = "en"
        });
    }

    private static void ApplyLanguage(string languageCode)
    {
        LocalizationService.Instance.ApplyCulture(ResolveCulture(languageCode));
    }
}
