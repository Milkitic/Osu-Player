using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OsuPlayer.Core.Configuration;
using OsuPlayer.Utils;
using OsuPlayer.ViewModels.Pages;
using OsuPlayer.ViewModels.Pages.Settings;

namespace OsuPlayer.Views.Pages;

public partial class SettingsPage : UserControl
{
    private readonly List<StackPanel> _sections = new();
    private bool _navigatingFromScroll;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _sections.Clear();
        _sections.Add(Section_General);
        _sections.Add(Section_Play);
        _sections.Add(Section_Interface);
        _sections.Add(Section_HotKey);
        _sections.Add(Section_Lyric);
        _sections.Add(Section_Export);
        _sections.Add(Section_About);

        ContentScroll.PropertyChanged += Scroll_PropertyChanged;
        ContentScroll.SizeChanged += (_, _) => UpdateStickyHeader();

        Dispatcher.Post(UpdateStickyHeader, DispatcherPriority.Background);
    }

    private void Scroll_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty)
        {
            UpdateStickyHeader();
        }
    }

    private void UpdateStickyHeader()
    {
        if (_sections.Count == 0 || ContentScroll == null) return;

        var offset = ContentScroll.Offset.Y;

        int currentIndex = -1;
        for (var i = 0; i < _sections.Count; i++)
        {
            var section = _sections[i];
            var sectionTop = section.Bounds.Top + offset;
            if (sectionTop <= offset + 1)
            {
                currentIndex = i;
            }
            else
            {
                break;
            }
        }

        if (currentIndex < 0)
        {
            StickyHeader.IsVisible = false;
            return;
        }

        var currentSection = _sections[currentIndex];
        var headerElement = currentSection.Children.Count > 0
            ? currentSection.Children[0]
            : null;

        if (headerElement is TextBlock tb)
        {
            StickyHeaderText.Text = tb.Text;
        }

        var sectionHeaderTop = currentSection.Bounds.Top + offset;
        if (sectionHeaderTop < 0)
        {
            StickyHeader.IsVisible = true;
        }
        else if (offset > 0)
        {
            StickyHeader.IsVisible = true;
        }
        else
        {
            StickyHeader.IsVisible = false;
        }

        if (!_navigatingFromScroll && DataContext is SettingsPageViewModel vm)
        {
            if (vm.SelectedIndex != currentIndex)
            {
                vm.SelectedIndex = currentIndex;
            }
        }
    }

    private void NavList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SettingsPageViewModel vm) return;
        if (NavList.SelectedIndex < 0 || NavList.SelectedIndex >= _sections.Count) return;
        if (_navigatingFromScroll) return;

        var target = _sections[NavList.SelectedIndex];
        var offset = target.Bounds.Top + ContentScroll.Offset.Y;
        _navigatingFromScroll = true;
        try
        {
            ContentScroll.Offset = new Vector(0, Math.Max(0, offset));
        }
        finally
        {
            _navigatingFromScroll = false;
        }
        UpdateStickyHeader();
    }

    private void HotKeyTextBox_GotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: HotKeyEntry entry })
        {
            entry.IsEditing = true;
        }
    }

    private void HotKeyTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotKeyEntry entry })
        {
            return;
        }

        entry.IsEditing = false;
        if (DataContext is SettingsPageViewModel vm)
        {
            vm.HotKey.RefreshEntries();
        }
    }

    private void HotKeyTextBox_KeyDown(object? sender, KeyEventArgs e)
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