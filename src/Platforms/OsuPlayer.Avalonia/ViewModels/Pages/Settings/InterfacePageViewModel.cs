using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core.Configuration;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class InterfacePageViewModel : ObservableObject
{
    public List<string> AvailableLanguages => I18NUtil.AvailableLangDic.Keys.ToList();

    private string? _selectedLanguage;
    public string SelectedLanguage
    {
        get => _selectedLanguage ?? I18NUtil.AvailableLangDic.FirstOrDefault(kv => kv.Value == (AppSettings.Default?.Interface.Locale ?? "en-US")).Key ?? "English";
        set
        {
            if (!SetProperty(ref _selectedLanguage, value) || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!I18NUtil.AvailableLangDic.TryGetValue(value, out var locale))
            {
                return;
            }

            I18NUtil.SwitchToLang(locale);
            if (AppSettings.Default != null)
            {
                AppSettings.Default.Interface.Locale = locale;
                AppSettings.SaveDefault();
            }
        }
    }

    public bool MinimalMode
    {
        get => AppSettings.Default?.Interface.MinimalMode == true;
        set
        {
            if (AppSettings.Default == null || AppSettings.Default.Interface.MinimalMode == value) return;
            AppSettings.Default.Interface.MinimalMode = value;
            OnPropertyChanged();
            AppSettings.SaveDefault();
        }
    }
}
