using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace OsuPlayer.Controls;

public class CommonTextBox : TextBox
{
    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<CommonTextBox, string?>(nameof(Hint));

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }
}
