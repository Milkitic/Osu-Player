using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Utils;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages.Settings;

public partial class HotKeyPage : UserControl
{
    public HotKeyPage()
    {
        InitializeComponent();
    }

    public HotKeyPage(HotKeyPageViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void TextBox_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: HotKeyEntry entry })
        {
            entry.IsEditing = true;
        }
    }

    private void TextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotKeyEntry entry })
        {
            return;
        }

        entry.IsEditing = false;
        if (DataContext is HotKeyPageViewModel viewModel)
        {
            viewModel.RefreshEntries();
        }
    }

    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotKeyEntry entry, Tag: HotKeyType type })
        {
            return;
        }

        entry.HotKeyText = HotKeyTextHelper.FormatPreview(e.KeyModifiers, e.Key);
        if (HotKeyTextHelper.IsModifierKey(e.Key))
        {
            return;
        }

        var appSettings = AppSettings.Default;
        if (appSettings == null)
        {
            return;
        }

        var hotKey = appSettings.HotKeys.FirstOrDefault(k => k.Type == type);
        if (hotKey == null)
        {
            hotKey = new HotKey { Type = type };
            appSettings.HotKeys.Add(hotKey);
        }

        if (!HotKeyTextHelper.TryConvert(e.Key, out var hookKey))
        {
            return;
        }

        hotKey.Enabled = true;
        hotKey.Key = hookKey;
        hotKey.UseControlKey = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        hotKey.UseShiftKey = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        hotKey.UseAltKey = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        entry.HotKeyText = HotKeyTextHelper.Format(hotKey);
        AppSettings.SaveDefault();
        e.Handled = true;
    }
}
