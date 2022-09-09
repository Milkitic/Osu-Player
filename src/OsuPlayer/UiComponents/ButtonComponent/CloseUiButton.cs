using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Milki.OsuPlayer.UiComponents.ButtonComponent;

public class CloseUiButton : SystemUiButton
{
    private const string ResourceCloseTemplString =
        "    <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Key=\"CloseTempl\">\r\n        <Canvas Width=\"1024\" Height=\"1024\">\r\n            <Path Fill=\"{TemplateBinding Foreground}\">\r\n                <Path.Data>\r\n                    <PathGeometry Figures=\"M886.592 841.344L557.248 512l329.36-329.36a32 32 0 1 0-45.264-45.232L512 466.752 182.656 137.408a32 32 0 1 0-45.264 45.232L466.752 512 137.408 841.344a32 32 0 1 0 45.232 45.264L512 557.248l329.36 329.36a32 32 0 1 0 45.232-45.264z\" FillRule=\"NonZero\" />\r\n                </Path.Data>\r\n            </Path>\r\n        </Canvas>\r\n    </ControlTemplate>";

    private static readonly ControlTemplate CloseTempl;
    private static readonly Brush StaticMouseOverBackground;
    private static readonly Brush StaticMouseDownBackground;

    static CloseUiButton()
    {
        CloseTempl = System.Windows.Markup.XamlReader.Parse(ResourceCloseTemplString) as ControlTemplate;
        StaticMouseOverBackground = (Brush)new BrushConverter().ConvertFrom("#F0F72F2F");
        StaticMouseDownBackground = (Brush)new BrushConverter().ConvertFrom("#A0C72828");
    }

    public CloseUiButton()
    {
        IconSize = 16;
        IconTemplate = CloseTempl;
        MouseOverBackground = StaticMouseOverBackground;
        MouseDownBackground = StaticMouseDownBackground;
        Click += OnClick;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        HostWindow?.Close();
    }
}