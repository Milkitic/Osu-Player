using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.TextBoxComponent;

public class CommonTextBox : TextBox
{
    public bool IsPopupOpened
    {
        get => (bool)GetValue(IsPopupOpenedProperty);
        set => SetValue(IsPopupOpenedProperty, value);
    }

    public string PopupText
    {
        get => (string)GetValue(PopupTextProperty);
        set => SetValue(PopupTextProperty, value);
    }

    public string HintText
    {
        get => (string)GetValue(HintTextProperty);
        set => SetValue(HintTextProperty, value);
    }

    public Brush HintTextForeground
    {
        get => (Brush)GetValue(HintTextForegroundProperty);
        set => SetValue(HintTextForegroundProperty, value);
    }

    public ControlTemplate IconTemplate
    {
        get => (ControlTemplate)GetValue(IconTemplateProperty);
        set => SetValue(IconTemplateProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Thickness IconMargin
    {
        get => (Thickness)GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Brush MouseOverBackground
    {
        get => (Brush)GetValue(MouseOverBackgroundProperty);
        set => SetValue(MouseOverBackgroundProperty, value);
    }

    public Brush MouseOverForeground
    {
        get => (Brush)GetValue(MouseOverForegroundProperty);
        set => SetValue(MouseOverForegroundProperty, value);
    }

    public Brush MouseDownBackground
    {
        get => (Brush)GetValue(MouseDownBackgroundProperty);
        set => SetValue(MouseDownBackgroundProperty, value);
    }

    public Brush MouseDownForeground
    {
        get => (Brush)GetValue(MouseDownForegroundProperty);
        set => SetValue(MouseDownForegroundProperty, value);
    }

    public bool AcceptOnlyNumberAndEnglish
    {
        get => (bool)GetValue(AcceptOnlyNumberAndEnglishProperty);
        set => SetValue(AcceptOnlyNumberAndEnglishProperty, value);
    }

    public Brush FocusBorderBrush
    {
        get => (Brush)GetValue(FocusBorderBrushProperty);
        set => SetValue(FocusBorderBrushProperty, value);
    }

    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        base.OnPreviewTextInput(e);
        if (AcceptOnlyNumberAndEnglish)
        {
            if (!IsNumberOrEnglish(e.Text))
            {
                e.Handled = true;
            }
        }
    }

    private static bool IsNumberOrEnglish(string str)
    {
        return Regex.IsMatch(str, @"^[A-Za-z0-9]+$");
    }

    public static readonly DependencyProperty IsPopupOpenedProperty = DependencyProperty.Register(nameof(IsPopupOpened), typeof(bool), typeof(CommonTextBox), new PropertyMetadata(default(bool)));
    public static readonly DependencyProperty PopupTextProperty = DependencyProperty.Register(nameof(PopupText), typeof(string), typeof(CommonTextBox), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty HintTextForegroundProperty = DependencyProperty.Register(nameof(HintTextForeground), typeof(Brush), typeof(CommonTextBox), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty HintTextProperty = DependencyProperty.Register(nameof(HintText), typeof(string), typeof(CommonTextBox), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty AcceptOnlyNumberAndEnglishProperty = DependencyProperty.Register(nameof(AcceptOnlyNumberAndEnglish), typeof(bool), typeof(CommonTextBox));
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(CommonTextBox), new PropertyMetadata(default(CornerRadius)));
    public static readonly DependencyProperty IconTemplateProperty = DependencyProperty.Register(nameof(IconTemplate), typeof(ControlTemplate), typeof(CommonTextBox), new PropertyMetadata(default(ControlTemplate)));
    public static readonly DependencyProperty IconMarginProperty = DependencyProperty.Register(nameof(IconMargin), typeof(Thickness), typeof(CommonTextBox), new PropertyMetadata(new Thickness(3)));
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(CommonTextBox), new PropertyMetadata(24d));
    public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register(nameof(MouseOverBackground), typeof(Brush), typeof(CommonTextBox), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register(nameof(MouseOverForeground), typeof(Brush), typeof(CommonTextBox), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownBackgroundProperty = DependencyProperty.Register(nameof(MouseDownBackground), typeof(Brush), typeof(CommonTextBox), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty MouseDownForegroundProperty = DependencyProperty.Register(nameof(MouseDownForeground), typeof(Brush), typeof(CommonTextBox), new PropertyMetadata(default(Brush)));
    public static readonly DependencyProperty FocusBorderBrushProperty = DependencyProperty.Register(nameof(FocusBorderBrush), typeof(Brush), typeof(CommonTextBox), new PropertyMetadata(default(Brush)));
}