using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace OsuPlayer.Views.UserControls;

/// <summary>
/// A single labelled slider + textbox pair for tweaking a numeric
/// parameter. The two-way binding between the slider and the textbox
/// is what makes the live "听参数" workflow usable: drag the slider
/// to scrub, or type a precise value into the textbox and the slider
/// snaps to it.
/// </summary>
public partial class ParamSliderRow : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ParamSliderRow, string>(nameof(Label));

    public static readonly StyledProperty<float> ValueProperty =
        AvaloniaProperty.Register<ParamSliderRow, float>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<float> MinimumProperty =
        AvaloniaProperty.Register<ParamSliderRow, float>(nameof(Minimum), -1f);

    public static readonly StyledProperty<float> MaximumProperty =
        AvaloniaProperty.Register<ParamSliderRow, float>(nameof(Maximum), 1f);

    public static readonly StyledProperty<float> TickFrequencyProperty =
        AvaloniaProperty.Register<ParamSliderRow, float>(nameof(TickFrequency), 0.01f);

    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<ParamSliderRow, string>(nameof(DisplayText), defaultBindingMode: BindingMode.TwoWay);

    public ParamSliderRow()
    {
        InitializeComponent();
        // Sync the textbox whenever the slider moves. The textbox
        // also pushes back into Value via its own TwoWay binding,
        // so we only need to format here.
        ValueProperty.Changed.AddClassHandler<ParamSliderRow>((row, _) => row.UpdateDisplayText());
        // Reformat if external code changes the value.
        UpdateDisplayText();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public float Value
    {
        get => GetValue(ValueProperty);
        set
        {
            // Round-trip through clamping so out-of-range textbox
            // entries get pulled back into the slider's domain.
            var clamped = Math.Clamp(value, Minimum, Maximum);
            if (Math.Abs(clamped - value) > float.Epsilon)
            {
                SetValue(ValueProperty, clamped);
                return;
            }
            SetValue(ValueProperty, value);
        }
    }

    public float Minimum
    {
        get => GetValue(MinimumProperty);
        set
        {
            SetValue(MinimumProperty, value);
            UpdateDisplayText();
        }
    }

    public float Maximum
    {
        get => GetValue(MaximumProperty);
        set
        {
            SetValue(MaximumProperty, value);
            UpdateDisplayText();
        }
    }

    public float TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public string DisplayText
    {
        get => GetValue(DisplayTextProperty);
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            // The user just typed something; parse and clamp.
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                var clamped = Math.Clamp(parsed, Minimum, Maximum);
                if (Math.Abs(clamped - Value) > float.Epsilon)
                {
                    SetValue(ValueProperty, clamped);
                }
                else
                {
                    UpdateDisplayText();
                }
            }
            else
            {
                UpdateDisplayText();
            }
        }
    }

    private void UpdateDisplayText()
    {
        // Choose the formatting precision based on tick spacing so
        // the textbox shows the same number of decimals the slider
        // snaps to.
        var tick = Math.Max(TickFrequency, 1e-4f);
        var decimals = Math.Max(0, (int)Math.Ceiling(-Math.Log10(tick)) + 1);
        if (decimals > 6) decimals = 6;
        var formatted = Value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        SetCurrentValue(DisplayTextProperty, formatted);
    }
}
