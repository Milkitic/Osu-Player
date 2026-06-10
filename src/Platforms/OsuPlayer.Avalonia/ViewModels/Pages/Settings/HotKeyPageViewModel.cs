using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Utils;

namespace OsuPlayer.ViewModels.Pages.Settings;

public partial class HotKeyPageViewModel : ObservableObject
{
    public ObservableCollection<HotKeyEntry> Entries { get; } =
    [
        new() { Type = HotKeyType.TogglePlay, DisplayName = "播放/暂停" },
        new() { Type = HotKeyType.PrevSong, DisplayName = "上一首" },
        new() { Type = HotKeyType.NextSong, DisplayName = "下一首" },
        new() { Type = HotKeyType.VolumeUp, DisplayName = "音量+" },
        new() { Type = HotKeyType.VolumeDown, DisplayName = "音量-" },
        new() { Type = HotKeyType.SwitchFullMiniMode, DisplayName = "切换迷你模式" },
        new() { Type = HotKeyType.AddCurrentToFav, DisplayName = "添加当前到收藏" },
        new() { Type = HotKeyType.SwitchLyricWindow, DisplayName = "开关桌面歌词" }
    ];

    public HotKeyPageViewModel()
    {
        RefreshEntries();
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
}

public partial class HotKeyEntry : ObservableObject
{
    public HotKeyType Type { get; set; }
    public string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string HotKeyText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEditing { get; set; }
}
