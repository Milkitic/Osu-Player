using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;

namespace Milki.OsuPlayer.Wpf;

public class WindowFrame : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty ChildProperty = DependencyProperty.Register("Child",
        typeof(object),
        typeof(WindowFrame),
        new PropertyMetadata(default(object)));

    public static readonly DependencyProperty IsMaxProperty = DependencyProperty.Register("IsMax",
        typeof(bool),
        typeof(WindowFrame),
        new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty HasMinProperty = DependencyProperty.Register("HasMin",
        typeof(bool),
        typeof(WindowFrame),
        new PropertyMetadata(true));

    public static readonly DependencyProperty HasMaxProperty = DependencyProperty.Register("HasMax",
        typeof(bool),
        typeof(WindowFrame),
        new PropertyMetadata(true));

    public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register("CanClose",
        typeof(bool),
        typeof(WindowFrame),
        new PropertyMetadata(true));

    private Window? _owner;

    public bool CanClose
    {
        get => (bool)GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    public bool HasMin
    {
        get => (bool)GetValue(HasMinProperty);
        set => SetValue(HasMinProperty, value);
    }

    public bool HasMax
    {
        get => (bool)GetValue(HasMaxProperty);
        set => SetValue(HasMaxProperty, value);
    }

    public bool IsMax
    {
        get => (bool)GetValue(IsMaxProperty);
        set => SetValue(IsMaxProperty, value);
    }

    public object Child
    {
        get => (object)GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public Window? Owner
    {
        get => _owner;
        internal set
        {
            if (Equals(value, _owner)) return;
            _owner = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}