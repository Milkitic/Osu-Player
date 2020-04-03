using Gma.System.MouseKeyHook;
using System;
using System.Linq;
using System.Windows.Forms;
using Milky.OsuPlayer.Common.Configuration;
using Milky.OsuPlayer.Pages.Settings;
using Milky.OsuPlayer.Windows;

namespace Milky.OsuPlayer
{
    public sealed class OverallKeyHook : IDisposable
    {
        private readonly MainWindow _mainWindow;
        private readonly IKeyboardMouseEvents _globalHook;
        private bool _holdingCtrl, _holdingAlt, _holdingShift;
        private HotKeyType? _configType;

        public HotKeyType? ConfigType
        {
            private get => _mainWindow.ConfigWindow != null && !_mainWindow.ConfigWindow.IsClosed &&
                   _mainWindow.ConfigWindow.MainFrame.Content is HotKeyPage
                ? _configType
                : null;
            set => _configType = value;
        }

        public OverallKeyHook(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _globalHook = Hook.GlobalEvents();
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
                setHotKey.Callback = callback;
            }
        }

        public static void BindHotKey(HotKeyType type, bool useCtrl, bool useAlt, bool useShift, Keys key)
        {
            var hotKey = AppSettings.Default.HotKeys.FirstOrDefault(k => k.Type == type);
            if (hotKey == null) throw new ArgumentException();
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
}
