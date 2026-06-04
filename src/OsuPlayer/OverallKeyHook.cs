using Milki.Extensions.MouseKeyHook;
using Milky.OsuPlayer.Pages.Settings;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.Windows;
using System;
using System.Linq;
using Milky.OsuPlayer.Core.Configuration;
using NLog;

namespace Milky.OsuPlayer
{
    public sealed class OverallKeyHook : IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MainWindow _mainWindow;
        private readonly IKeyboardHook _globalHook;
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
            _globalHook = KeyboardHookFactory.CreateGlobal();
            _globalHook.KeyPressed += GlobalHookKeyPressed;
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
                if (setHotKey.Key != HookKeys.None) 
                { 
                    setHotKey.Callback = callback; 
                }
            }
        }

        public static void BindHotKey(HotKeyType type, bool useCtrl, bool useAlt, bool useShift, HookKeys key)
        {
            var hotKey = AppSettings.Default.HotKeys.FirstOrDefault(k => k.Type == type);
            if (hotKey == null)
            {
                Logger.Warn("HotKey shouldn't be null.");
                return;
            }

            hotKey.Key = key;
            hotKey.UseControlKey = useCtrl;
            hotKey.UseAltKey = useAlt;
            hotKey.UseShiftKey = useShift;
        }

        private void GlobalHookKeyPressed(HookModifierKeys modifiers, HookKeys key, KeyAction action)
        {
            if (action != KeyAction.KeyDown || IsModifierKey(key))
            {
                return;
            }

            Execute.ToUiThread(() => HandleKeyPressed(modifiers, key));
        }

        private void HandleKeyPressed(HookModifierKeys modifiers, HookKeys key)
        {
            var useCtrl = modifiers.HasFlag(HookModifierKeys.Control);
            var useAlt = modifiers.HasFlag(HookModifierKeys.Alt);
            var useShift = modifiers.HasFlag(HookModifierKeys.Shift);

            var configType = ConfigType;
            if (configType != null)
            {
                BindHotKey(configType.Value, useCtrl, useAlt, useShift, key);
            }
            else
            {
                AppSettings.Default.HotKeys.FirstOrDefault(hotKey =>
                    useCtrl == hotKey.UseControlKey && useAlt == hotKey.UseAltKey &&
                    useShift == hotKey.UseShiftKey && key == hotKey.Key)?.Callback?.Invoke();
            }
        }

        private static bool IsModifierKey(HookKeys key)
        {
            return key is HookKeys.ControlKey or HookKeys.LControlKey or HookKeys.RControlKey
                or HookKeys.ShiftKey or HookKeys.LShiftKey or HookKeys.RShiftKey
                or HookKeys.Menu or HookKeys.LMenu or HookKeys.RMenu;
        }

        public void Dispose()
        {
            _globalHook.KeyPressed -= GlobalHookKeyPressed;
            _globalHook.Dispose();
        }
    }
}
