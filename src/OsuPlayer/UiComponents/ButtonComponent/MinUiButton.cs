using System.Windows;
using System.Windows.Controls;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

public class MinUiButton : SystemUiButton
{
    private const string ResourceMinimizeTemplString =
        "    <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key=\"MinimizeTempl\">\r\n        <Canvas Width=\"1024\" Height=\"1024\">\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M797.291117 486.21473 224.18848 486.21473c-14.078647 0-25.469068 11.342326-25.469068 25.472138 0 14.028505 11.390421 25.471115 25.469068 25.471115l573.101613 0c14.07967 0 25.470091-11.441587 25.470091-25.471115C822.760185 497.557056 811.370787 486.21473 797.291117 486.21473z\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n        </Canvas>\r\n    </ControlTemplate>";

    private static readonly ControlTemplate MinimizeTempl;

    static MinUiButton()
    {
        MinimizeTempl = System.Windows.Markup.XamlReader.Parse(ResourceMinimizeTemplString) as ControlTemplate;
    }

    public MinUiButton()
    {
        IconMargin = new Thickness(0, 8, 0, 0);
        IconTemplate = MinimizeTempl;
        Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (HostWindow != null)
        {
            HostWindow.WindowState = WindowState.Minimized;
        }
    }
}