#nullable enable
using Milki.Extensions.MouseKeyHook;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Shared.Models;

namespace Milki.OsuPlayer.Services;

public sealed class KeyHookService : IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly IKeyboardHook _keyboardHook;

    private readonly List<Guid> _registerList = new();

    public KeyHookService(AppSettings appSettings)
    {
        _appSettings = appSettings;
        _keyboardHook = KeyboardHookFactory.CreateGlobal();
    }

    public Action? AddCurrentToFavAction { get; set; }
    public Action? NextSongAction { get; set; }
    public Action? PrevSongAction { get; set; }
    public Action? SwitchFullMiniModeAction { get; set; }
    public Action? SwitchLyricWindowAction { get; set; }
    public Action? TogglePlayAction { get; set; }
    public Action? VolumeDownAction { get; set; }
    public Action? VolumeUpAction { get; set; }

    public void Dispose()
    {
        _keyboardHook.Dispose();
    }

    public void InitializeAndActivateHotKeys()
    {
        RegisterKey(_appSettings.HotKeys.TogglePlay, () => TogglePlayAction);
        RegisterKey(_appSettings.HotKeys.PrevSong, () => PrevSongAction);
        RegisterKey(_appSettings.HotKeys.NextSong, () => NextSongAction);
        RegisterKey(_appSettings.HotKeys.VolumeUp, () => VolumeUpAction);
        RegisterKey(_appSettings.HotKeys.VolumeDown, () => VolumeDownAction);
        RegisterKey(_appSettings.HotKeys.SwitchFullMiniMode, () => SwitchFullMiniModeAction);
        RegisterKey(_appSettings.HotKeys.AddCurrentToFav, () => AddCurrentToFavAction);
        RegisterKey(_appSettings.HotKeys.SwitchLyricWindow, () => SwitchLyricWindowAction);
    }

    public void DeactivateHotKeys()
    {
        foreach (var guid in _registerList)
        {
            _keyboardHook.TryUnregister(guid);
        }

        _registerList.Clear();
    }

    private void RegisterKey(BindKeys? bindKeys, Func<Action?> getAction)
    {
        if (bindKeys?.Keys is not { } keys) return;
        var modifier = bindKeys.ModifierKeys;
        if (modifier == HookModifierKeys.None)
        {
            _registerList.Add(_keyboardHook.RegisterKeyDown(keys, KeyboardCallback));
        }
        else
        {
            _registerList.Add(_keyboardHook.RegisterHotkey(modifier, keys, KeyboardCallback));
        }

        void KeyboardCallback(HookModifierKeys hookModifierKeys, HookKeys hookKeys, KeyAction keyAction)
        {
            getAction()?.Invoke();
        }
    }
}