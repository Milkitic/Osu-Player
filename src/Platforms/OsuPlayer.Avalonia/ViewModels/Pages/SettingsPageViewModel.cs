using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Lang;
using OsuPlayer.ViewModels.Pages.Settings;
using SettingsExportPageViewModel = OsuPlayer.ViewModels.Pages.Settings.ExportPageViewModel;

namespace OsuPlayer.ViewModels.Pages;

public partial class SettingsPageViewModel : ObservableObject
{
    public GeneralPageViewModel General { get; }
    public PlayPageViewModel Play { get; }
    public InterfacePageViewModel Interface { get; }
    public HotKeyPageViewModel HotKey { get; }
    public LyricPageViewModel Lyric { get; }
    public SettingsExportPageViewModel Export { get; }
    public AboutPageViewModel About { get; }

    public IReadOnlyList<SettingsNavItem> NavItems { get; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public SettingsPageViewModel(
        GeneralPageViewModel general,
        PlayPageViewModel play,
        InterfacePageViewModel @interface,
        HotKeyPageViewModel hotKey,
        LyricPageViewModel lyric,
        SettingsExportPageViewModel export,
        AboutPageViewModel about)
    {
        General = general;
        Play = play;
        Interface = @interface;
        HotKey = hotKey;
        Lyric = lyric;
        Export = export;
        About = about;

        NavItems = new List<SettingsNavItem>
        {
            new("General", SRKeys.Ui_Sets_Nav_Common),
            new("Play", SRKeys.Ui_Sets_Nav_Playing),
            new("Interface", SRKeys.Ui_Sets_Nav_Interface),
            new("HotKey", SRKeys.Ui_Sets_Nav_HotKey),
            new("Lyric", SRKeys.Ui_Sets_Nav_Lyric),
            new("Export", SRKeys.Ui_Sets_Nav_Export),
            new("About", SRKeys.Ui_Sets_Nav_About),
        };
    }
}

public sealed record SettingsNavItem(string Tag, string LabelKey);