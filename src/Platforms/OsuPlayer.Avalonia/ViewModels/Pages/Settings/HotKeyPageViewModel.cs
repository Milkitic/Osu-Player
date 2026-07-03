using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Lang;
using OsuPlayer.Localization;
using OsuPlayer.Utils;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class HotKeyPageViewModel : ObservableObject, IDisposable
{
    public ObservableCollection<HotKeyEntry> Entries { get; } =
    [
        new(HotKeyType.TogglePlay, () => $"{Text(SRKeys.Ui_Play)} / {Text(SRKeys.Ui_Pause)}"),
        new(HotKeyType.PrevSong, () => Text(SRKeys.Ui_PrevMusic)),
        new(HotKeyType.NextSong, () => Text(SRKeys.Ui_NextMusic)),
        new(HotKeyType.VolumeUp, () => Text(SRKeys.Ui_VolumeUp)),
        new(HotKeyType.VolumeDown, () => Text(SRKeys.Ui_VolumeDown)),
        new(HotKeyType.SwitchFullMiniMode, () => $"{Text(SRKeys.Ui_Switch)}{Text(SRKeys.Ui_MiniMode)}"),
        new(HotKeyType.AddCurrentToFav, () => Text(SRKeys.Ui_Sets_Content_AddToFavorite)),
        new(HotKeyType.SwitchLyricWindow, () => $"{Text(SRKeys.Ui_Open)} / {Text(SRKeys.Ui_Close)}{Text(SRKeys.Ui_DesktopLyric)}")
    ];

    public HotKeyPageViewModel()
    {
        LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
        RefreshEntries();
    }

    private static string Text(string key) => LocalizationService.Instance[key];

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LocalizationService.Version) or "Item[]")
        {
            RefreshDisplayNames();
        }
    }

    private void RefreshDisplayNames()
    {
        foreach (var entry in Entries)
        {
            entry.RefreshDisplayName();
        }
    }

    public void RefreshEntries()
    {
        var appSettings = AppSettings.Default;
        if (appSettings == null)
        {
            return;
        }

        foreach (var entry in Entries)
        {
            var hotKey = appSettings.HotKeys.FirstOrDefault(k => k.Type == entry.Type) ??
                         new HotKey { Type = entry.Type };
            entry.HotKeyText = HotKeyTextHelper.Format(hotKey);
        }
    }

    public void Dispose()
    {
        LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
    }
}

public partial class HotKeyEntry : ObservableObject
{
    private readonly Func<string> _displayNameFactory;

    public HotKeyEntry(HotKeyType type, Func<string> displayNameFactory)
    {
        Type = type;
        _displayNameFactory = displayNameFactory;
        DisplayName = _displayNameFactory();
    }

    public HotKeyType Type { get; }

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string HotKeyText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEditing { get; set; }

    public void RefreshDisplayName()
    {
        DisplayName = _displayNameFactory();
    }
}
