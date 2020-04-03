using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.UiComponent.TextBoxComponent
{
    public class CommonTextBox : TextBox
    {

        private static CommonTextBox _lastTextBox;

        public CommonTextBox()
        {
            LostFocus += (sender, e) =>
            {
                if (_lastTextBox != this && _lastTextBox != null)
                {
                    _lastTextBox.SelectionLength = 0;
                }

                _lastTextBox = this;
            };
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }

        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(
                "Hint",
                typeof(string),
                typeof(CommonTextBox),
                null
            );

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(CommonTextBox),
                null
            );
        public ControlTemplate IconTemplate
        {
            get => (ControlTemplate)GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        public static readonly DependencyProperty IconTemplateProperty =
            DependencyProperty.Register(
                "IconTemplate",
                typeof(ControlTemplate),
                typeof(CommonTextBox),
                null
            );
    }
}
