using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Shared;
using OsuPlayer.Shared.Models;

namespace OsuPlayer.Avalonia.ViewModels.Pages.Settings;

public partial class HotKeyPageViewModel : ObservableObject
{
    public List<HotKeyEntry> Entries { get; } = new()
    {
        new() { Type = HotKeyType.TogglePlay, DisplayName = "播放/暂停" },
        new() { Type = HotKeyType.PrevSong, DisplayName = "上一首" },
        new() { Type = HotKeyType.NextSong, DisplayName = "下一首" },
        new() { Type = HotKeyType.VolumeUp, DisplayName = "音量+" },
        new() { Type = HotKeyType.VolumeDown, DisplayName = "音量-" },
        new() { Type = HotKeyType.SwitchFullMiniMode, DisplayName = "切换迷你模式" },
        new() { Type = HotKeyType.AddCurrentToFav, DisplayName = "添加当前到收藏" },
        new() { Type = HotKeyType.SwitchLyricWindow, DisplayName = "开关桌面歌词" }
    };

    [RelayCommand]
    private void StartConfig(HotKeyType type)
    {
        // TODO: 集成到 OverallKeyHook
    }
}

public class HotKeyEntry
{
    public HotKeyType Type { get; set; }
    public string DisplayName { get; set; } = "";
    public string HotKeyText { get; set; } = "";
}
