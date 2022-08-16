#nullable enable
using System;
using System.Linq;
using System.Windows.Forms;
using Anotar.NLog;
using Milki.OsuPlayer.Configuration;

namespace Milki.OsuPlayer.Services;

public sealed class KeyHookService : IDisposable
{
    private readonly IKeyboardMouseEvents _globalHook;
    private bool _holdingCtrl, _holdingAlt, _holdingShift;

    public HotKeyType? ConfigType { get; set; }

    public KeyHookService()
    {
        _globalHook.KeyDown += GlobalHookKeyDown;
        _globalHook.KeyUp += GlobalHookKeyUp;
    }

    public static void AddKeyHook(HotKeyType type, Action callback)
    {
        var setHotKey = AppSettings.Default.HotKeys.FirstOrDefault(k => k.Type == type);
        if (setHotKey == null)
        {
            setHotKey = new HotKey { Type = type, Callback = callback };
            AppSettings.Default.HotKeys.Add(setHotKey);
        }
        else
        {
            if (setHotKey.Key != Keys.None)
            {
                setHotKey.Callback = callback;
            }
        }
    }

    public static void BindHotKey(HotKeyType type, bool useCtrl, bool useAlt, bool useShift, Keys key)
    {
        var hotKey = AppSettings.Default.HotKeys.FirstOrDefault(k => k.Type == type);
        if (hotKey == null)
        {
            LogTo.Warn("HotKey shouldn't be null.");
            return;
        }

        hotKey.Key = key;
        hotKey.UseControlKey = useCtrl;
        hotKey.UseAltKey = useAlt;
        hotKey.UseShiftKey = useShift;
    }

    private void GlobalHookKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            _holdingCtrl = true;
        else if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            _holdingShift = true;
        else if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            _holdingAlt = true;
        else
        {
            if (ConfigType != null)
            {
                BindHotKey(ConfigType.Value, _holdingCtrl, _holdingAlt, _holdingShift, e.KeyCode);
            }
            else
                AppSettings.Default.HotKeys.FirstOrDefault(key =>
                    _holdingCtrl == key.UseControlKey && _holdingAlt == key.UseAltKey &&
                    _holdingShift == key.UseShiftKey && e.KeyCode == key.Key)?.Callback?.Invoke();
        }
    }

    private void GlobalHookKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            _holdingCtrl = false;
        else if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            _holdingShift = false;
        else if (e.KeyCode == Keys.LMenu || e.KeyCode == Keys.RMenu)
            _holdingAlt = false;
        else
        {
            // ignore
        }
    }

    public void Dispose()
    {
        _globalHook.KeyDown -= GlobalHookKeyDown;
        _globalHook.KeyUp -= GlobalHookKeyUp;
        _globalHook.Dispose();
    }
}