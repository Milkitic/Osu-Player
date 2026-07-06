using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OsuPlayer.Controls.MessageDialogs;

public class TaskDialogConstants : INotifyPropertyChanged
{
    private string _buttonOkText = "确定(_O)";
    private string _buttonCancelText = "取消(_C)";
    private string _buttonYesText = "是(_Y)";
    private string _buttonNoText = "否(_N)";
    private string _footerDetailText = "详细信息:";

    private TaskDialogConstants()
    {
    }

    public static TaskDialogConstants Instance { get; } = new();

    public string ButtonOkText
    {
        get => _buttonOkText;
        set => SetField(ref _buttonOkText, value);
    }

    public string ButtonCancelText
    {
        get => _buttonCancelText;
        set => SetField(ref _buttonCancelText, value);
    }

    public string ButtonYesText
    {
        get => _buttonYesText;
        set => SetField(ref _buttonYesText, value);
    }

    public string ButtonNoText
    {
        get => _buttonNoText;
        set => SetField(ref _buttonNoText, value);
    }

    public string FooterDetailText
    {
        get => _footerDetailText;
        set => SetField(ref _footerDetailText, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
