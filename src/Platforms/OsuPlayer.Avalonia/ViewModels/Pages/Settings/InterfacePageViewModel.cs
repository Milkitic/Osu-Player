using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Localization;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class InterfacePageViewModel : ObservableObject
{
    public InterfacePageViewModel(LanguageManager languageManager)
    {
        LanguageManager = languageManager;
    }

    public LanguageManager LanguageManager { get; }

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
